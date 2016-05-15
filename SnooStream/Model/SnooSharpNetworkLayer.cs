using Newtonsoft.Json;
using SnooSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace SnooStream.Model
{

    class SnooSharpNetworkLayer : SnooSharp.INetworkLayer, IDisposable
    {
        HttpClient _httpClient;
        UserState _userState;
        private int _failedRequestCount = 0;
        static DateTime _priorRequestSet = new DateTime();
        static int _requestSetCount = 0;
        static DateTime _lastRequestMade = new DateTime();
        string _appId;
        string _appSecret;
        string _redirectUrl;

        public SnooSharpNetworkLayer(UserState userState, string appId, string appSecret, string redirectUrl)
        {
            _userState = userState ?? new UserState();
            _appId = appId;
            _appSecret = appSecret;
            _redirectUrl = redirectUrl;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.MaxForwards = 100;
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SnooStream/1.0");
        }

        public async Task<string> Get(string url, CancellationToken token, IProgress<float> progress, Dictionary<string, string> body)
        {
            await ThrottleRequests(token);
            await EnsureRedditCookie(token);
            var requestUri = new Uri(RedditBaseUrl + (url.Contains("://") ? new Uri(url).PathAndQuery : url));
            HttpRequestMessage sendMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);

            if (body != null)
            {
                
                sendMessage.Content = new HttpFormUrlEncodedContent(body);
            }

            var responseMessage = await _httpClient.SendRequestAsync(sendMessage, HttpCompletionOption.ResponseContentRead).AsTask(token, MakeHttpProgress(progress, url));

            if (!responseMessage.IsSuccessStatusCode && sendMessage.RequestUri != requestUri)
            {
                return await Get(sendMessage.RequestUri.PathAndQuery, token, progress, body);
            }
            else
            {
                var bodyString = ProcessJsonErrors(await responseMessage.Content.ReadAsStringAsync());
                if (bodyString.StartsWith("<!doctype html><html><title>") && bodyString.EndsWith("try again and hopefully we will be fast enough this time."))
                    return await Get(url, token, progress, body);
                else if (responseMessage.IsSuccessStatusCode)
                {
                    if (string.IsNullOrWhiteSpace(bodyString) || bodyString == "{}" || bodyString == "\"{}\"")
                        throw new RedditEmptyException("body string was empty but no error code was present");
                    else
                    {
                        _failedRequestCount = 0;
                        return bodyString;
                    }
                }
                else
                {
                    _failedRequestCount++;
                    switch (responseMessage.StatusCode)
                    {
                        case HttpStatusCode.GatewayTimeout:
                        case HttpStatusCode.RequestTimeout:
                        case HttpStatusCode.BadGateway:
                        case HttpStatusCode.BadRequest:
                        case HttpStatusCode.InternalServerError:
                        case HttpStatusCode.ServiceUnavailable:
                            {
                                if (_failedRequestCount < 5)
                                    return await Get(url, token, progress, body);
                                else
                                {
                                    switch (responseMessage.StatusCode)
                                    {
                                        case HttpStatusCode.InternalServerError:
                                        case HttpStatusCode.ServiceUnavailable:
                                            throw new RedditException("server down");
                                    }
                                }
                                break;
                            }
                        case HttpStatusCode.NotFound:
                            //reddit likes to return 404 for no apparent reason
                            if (_failedRequestCount < 2)
                                return await Get(url, token, progress, body);
                            else
                                throw new RedditNotFoundException(url);
                        case HttpStatusCode.Forbidden:
                            throw new RedditUnauthorizedException(url);
                    }
                    responseMessage.EnsureSuccessStatusCode();
                    return null;
                }
            }
        }

        public async Task Send(string url, string method, CancellationToken token, Dictionary<string, string> arguments)
        {
            await ThrottleRequests(token);
            await EnsureRedditCookie(token);
            HttpResponseMessage responseMessage = null;
            var request = new HttpRequestMessage(new HttpMethod(method), new Uri(url));
            request.Content = new HttpFormUrlEncodedContent(arguments);
            responseMessage = await _httpClient.SendRequestAsync(request).AsTask(token);
            await ProcessJsonErrors(responseMessage);
        }

        public async Task<string> Post(string url, CancellationToken token, Dictionary<string, string> arguments, IProgress<float> progress)
        {
            await ThrottleRequests(token);
            await EnsureRedditCookie(token);
            HttpResponseMessage responseMessage = null;
            var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url));
            request.Content = new HttpFormUrlEncodedContent(arguments);
            responseMessage = await _httpClient.SendRequestAsync(request, HttpCompletionOption.ResponseContentRead).AsTask(token, MakeHttpProgress(progress, url));
            return ProcessJsonErrors(await responseMessage.Content.ReadAsStringAsync());
        }

        private IProgress<HttpProgress> MakeHttpProgress(IProgress<float> progress, string uri)
        {
            if (progress is AggregateProgress)
                return ((AggregateProgress)progress).MakeHttpProgress(uri);
            else
                return new Progress<HttpProgress>();
        }

        public string RedditBaseUrl
        {
            get
            {
                if (_userState != null && _userState.OAuth != null)
                    return "https://oauth.reddit.com";
                else
                    return "http://www.reddit.com";
            }
        }

        private string ProcessJsonErrors(string response)
        {
            string realErrorString = "";
            try
            {
                if (response.Contains("errors"))
                {
                    var jsonErrors = JsonConvert.DeserializeObject<JsonErrorsData>(response);
                    if (jsonErrors.Errors != null && jsonErrors.Errors.Length > 0)
                    {
                        realErrorString = jsonErrors.Errors[0].ToString();
                    }
                }

            }
            catch
            {
            }
            if (!string.IsNullOrWhiteSpace(realErrorString))
                throw new RedditException(realErrorString);

#if DEBUG
            try
            {
                JsonConvert.DeserializeObject(response);
            }
            catch (Exception ex)
            {
                //throw new RedditException(ex.ToString());
            }
#endif
            return response;
        }

        private async Task ProcessJsonErrors(HttpResponseMessage httpResponse)
        {
            var response = await httpResponse.Content.ReadAsStringAsync();
            string realErrorString = "";
            try
            {
                if (response.Contains("errors"))
                {
                    var jsonErrors = JsonConvert.DeserializeObject<JsonErrorsData>(response);
                    if (jsonErrors.Errors != null && jsonErrors.Errors.Length > 0)
                    {
                        realErrorString = jsonErrors.Errors[0].ToString();
                    }
                }
            }
            catch
            {
            }
            if (!string.IsNullOrWhiteSpace(realErrorString))
                throw new RedditException(realErrorString);

#if DEBUG
            try
            {
                JsonConvert.DeserializeObject(response);
            }
            catch (Exception ex)
            {
                throw new RedditException(ex.ToString());
            }
#endif
        }

        private async Task EnsureRedditCookie(CancellationToken token)
        {
            if (_userState.OAuth != null)
            {
                //see if we need to refresh the token
                if (_userState.OAuth.Created.AddSeconds(_userState.OAuth.ExpiresIn) < DateTime.UtcNow)
                {
                    _userState.OAuth = await RefreshToken(_userState.OAuth.RefreshToken, token);
                }

                _httpClient.DefaultRequestHeaders.Authorization = new Windows.Web.Http.Headers.HttpCredentialsHeaderValue("Bearer", _userState.OAuth.AccessToken);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        public async Task<RedditOAuth> RequestGrantCode(string code, CancellationToken token)
        {
            //we're messing with the headers here so use a different client
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new Windows.Web.Http.Headers.HttpCredentialsHeaderValue("Basic", Convert.ToBase64String(System.Text.UTF8Encoding.UTF8.GetBytes(string.Format("{0}:{1}", _appId, _appSecret))));
            var result = await httpClient.PostAsync(new Uri("https://ssl.reddit.com/api/v1/access_token"), new HttpFormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"grant_type", "authorization_code"},
                    {"code", code},
                    {"redirect_uri", _redirectUrl}, //this is basically just a magic string that needs to match with reddit's app registry
				})).AsTask(token);
            var jsonResult = await result.Content.ReadAsStringAsync();
            var oAuth = JsonConvert.DeserializeObject<RedditOAuth>(jsonResult);
            oAuth.Created = DateTime.UtcNow;
            return oAuth;
        }

        public async Task DestroyToken(string refreshToken)
        {
            //we're messing with the headers here so use a different client
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new Windows.Web.Http.Headers.HttpCredentialsHeaderValue("Basic", Convert.ToBase64String(System.Text.UTF8Encoding.UTF8.GetBytes(string.Format("{0}:{1}", _appId, _appSecret))));
            var result = await httpClient.PostAsync(new Uri("https://ssl.reddit.com/api/v1/revoke_token"), new HttpFormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"token", refreshToken},
                    {"token_type_hint", "refresh_token"},
                }));
        }

        public async Task<RedditOAuth> RefreshToken(string refreshToken, CancellationToken token)
        {
            //we're messing with the headers here so use a different client
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new Windows.Web.Http.Headers.HttpCredentialsHeaderValue("Basic", Convert.ToBase64String(System.Text.UTF8Encoding.UTF8.GetBytes(string.Format("{0}:{1}", _appId, _appSecret))));
            var result = await httpClient.PostAsync(new Uri("https://ssl.reddit.com/api/v1/access_token"), new HttpFormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"grant_type", "refresh_token"},
                    {"refresh_token", refreshToken},
                })).AsTask(token);
            var jsonResult = await result.Content.ReadAsStringAsync();
            var oAuth = JsonConvert.DeserializeObject<RedditOAuth>(jsonResult);
            oAuth.Created = DateTime.UtcNow;
            oAuth.RefreshToken = refreshToken; //this is to make life a bit easier, since you would need to keep track of this thing anyway
            return oAuth;
        }

        //dont hammer reddit!
        //Make no more than thirty requests per minute. This allows some burstiness to your requests, 
        //but keep it sane. On average, we should see no more than one request every two seconds from you.
        //the above statement is from the reddit api docs, but its not quite true, there are some api's that have logging 
        //set for 15 requests in 30 seconds, so we can allow some burstiness but it must fit in the 15 requests/30 seconds rule
        public static async Task ThrottleRequests(CancellationToken token)
        {
            var offset = DateTime.Now - _lastRequestMade;
            if (offset.TotalMilliseconds < 1000)
            {
                await Task.Delay(1000 - (int)offset.TotalMilliseconds);
            }

            if (_requestSetCount > 15)
            {
                var overallOffset = DateTime.Now - _priorRequestSet;

                if (overallOffset.TotalSeconds < 30)
                {
                    var delay = (30 - (int)overallOffset.TotalSeconds) * 1000;
                    if (delay > 2)
                    {
                        for (int i = 0; i < delay; i++)
                        {
                            await Task.Delay(1000);
                        }
                    }
                    else
                        await Task.Delay(delay);
                }
                _requestSetCount = 0;
                _priorRequestSet = DateTime.Now;
            }
            _requestSetCount++;

            _lastRequestMade = DateTime.Now;
        }

        public SnooSharp.INetworkLayer Clone(RedditOAuth credential)
        {
            return new SnooSharpNetworkLayer(new UserState { OAuth = credential }, _appId, _appSecret, _redirectUrl);
        }

        public void Dispose()
        {
            if (_httpClient != null)
                _httpClient.Dispose();
            _httpClient = null;
        }
    }

    public class AggregateProgress : Progress<float>, IProgress<HttpProgress>
    {
        float _currentProgress = 0.0f;
        List<string> _httpLoadHistory = new List<string>();
        Action<float> _updated;

        public AggregateProgress(Action<float> updated)
        {
            _updated = updated;
        }

        public IProgress<HttpProgress> MakeHttpProgress(string uri)
        {
            _httpLoadHistory.Add(uri);
            return this;
        }

        public void Report(HttpProgress value)
        {
            var totalToReceive = value.TotalBytesToReceive ?? 0;
            if (totalToReceive > 0)
            {
                OnReport(value.BytesReceived / totalToReceive);
            }
        }

        protected override void OnReport(float value)
        {
            if (_httpLoadHistory.Count > 1)
            {
                var baseline = ((float)_httpLoadHistory.Count) / (float)(_httpLoadHistory.Count - 1);
                _currentProgress = baseline + ((1.0f - baseline) * value);
            }
            else
            {
                _currentProgress = value;
            }
            _updated(_currentProgress);
        }
    }
}
