using CommonResourceAcquisition.ImageAcquisition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace SnooStream.Model
{
    public interface INetworkLayer : IDisposable
    {
        Task<string> Get(string url, CancellationToken token, IProgress<float> progress, Dictionary<string, string> body);
        Task<string> CacheableGet(string url, CancellationToken token, IProgress<float> progress, Dictionary<string, string> body);
        void AddHeaders(string name, string value);
        void SetReferer(string referrer);
        INetworkLayer Clone();
    }

    public class PlainNetworkLayer : INetworkLayer, IResourceNetworkLayer
    {
        HttpClient _httpClient;
        public PlainNetworkLayer()
        {
            //mascarade as a mobile browser because those are usually cleaner pages
            var userAgent = "Mozilla/5.0 (Mobile; Windows Phone 8.1; Android 4.0; ARM; Trident/7.0; Touch; rv:11.0; IEMobile/11.0; NOKIA; Lumia 930) like iPhone OS 7_0_3 Mac OS X AppleWebKit/537 (KHTML, like Gecko) Mobile Safari/537";
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        }

        public void AddHeaders(string name, string value)
        {
            if (name == "User-Agent")
            {
                _httpClient.DefaultRequestHeaders.UserAgent.Clear();
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(value);
            }
            else
                _httpClient.DefaultRequestHeaders.Add(name, value);
        }

        public async Task<string> CacheableGet(string url, CancellationToken token, IProgress<float> progress, Dictionary<string, string> body)
        {
            //TODO check Kitaro for cache on this url
            //failing that set the http request to be as cachable as possible
            return await Get(url, token, progress, body);
        }

        public INetworkLayer Clone()
        {
            var result = new PlainNetworkLayer();
            foreach (var header in _httpClient.DefaultRequestHeaders)
                result.AddHeaders(header.Key, header.Value);
            
            if(_httpClient.DefaultRequestHeaders.Referer != null)
                result.SetReferer(_httpClient.DefaultRequestHeaders.Referer.ToString());
            return result;
        }

        public void Dispose()
        {
            if(_httpClient != null)
                _httpClient.Dispose();
            _httpClient = null;
        }

        public async Task<string> Get(string url, CancellationToken token, IProgress<float> progress, Dictionary<string, string> body)
        {
            return await Get(url, token, progress, body, false);
        }

        public async Task<string> Get(string url, CancellationToken token, IProgress<float> progress, Dictionary<string, string> body, bool ignoreErrors)
        {
            HttpRequestMessage sendMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(url));

            if (body != null)
            {

                sendMessage.Content = new HttpFormUrlEncodedContent(body);
            }

            var responseMessage = await _httpClient.SendRequestAsync(sendMessage, HttpCompletionOption.ResponseContentRead).AsTask(token, MakeHttpProgress(progress, url));

            if (!ignoreErrors)
                responseMessage.EnsureSuccessStatusCode();

            return await responseMessage.Content.ReadAsStringAsync();
        }

        public void JoinProgress(IProgress<float> progress1, IProgress<float> progress2)
        {
            if (progress1 is Progress<float>)
            {
                ((Progress<float>)progress1).ProgressChanged += (sender, arg) => progress2.Report(arg);
            }
        }

        public void SetReferer(string referrer)
        {
            _httpClient.DefaultRequestHeaders.Referer = new Uri(referrer);
        }

        IResourceNetworkLayer IResourceNetworkLayer.Clone()
        {
            return this.Clone() as IResourceNetworkLayer;
        }

        private IProgress<HttpProgress> MakeHttpProgress(IProgress<float> progress, string uri)
        {
            if (progress is AggregateProgress)
                return ((AggregateProgress)progress).MakeHttpProgress(uri);
            else
                return new Progress<HttpProgress>();
        }
    }
}
