using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Shell;
using Nokia.Graphics.Imaging;
using Nokia.InteropServices.WindowsRuntime;
using SnooStream.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Windows.Foundation;
using Windows.Networking.Connectivity;
using Windows.Storage.Streams;
using Windows.System.Threading;

namespace SnooStreamWP8.PlatformServices
{
    class SystemServices : ISystemServices
    {
        private Dispatcher _uiDispatcher;
        public SystemServices(Dispatcher uiDispatcher)
        {
            _uiDispatcher = uiDispatcher;
            NetworkInformation.NetworkStatusChanged += networkStatusChanged;
            networkStatusChanged(null);
        }

        private void networkStatusChanged(object sender)
        {
            _lowPriorityNetworkOk = new Lazy<bool>(LowPriorityNetworkOkImpl);
            _highPriorityNetworkOk = new Lazy<bool>(IsHighPriorityNetworkOkImpl);
        }

        public void StopTimer(object tickHandle)
        {
            if (tickHandle is DispatcherTimer)
            {
                if (((DispatcherTimer)tickHandle).IsEnabled)
                    ((DispatcherTimer)tickHandle).Stop();
            }
            else if (tickHandle is Task<DispatcherTimer>)
            {
                _uiDispatcher.BeginInvoke(async () =>
                {
                    var timer = await (Task<DispatcherTimer>)tickHandle;
                    timer.Stop();
                });
            }
            else if (tickHandle is ThreadPoolTimer)
            {
                ((ThreadPoolTimer)tickHandle).Cancel();
            }
        }

        public async void RunAsync(Func<object, Task> action)
        {
            await AsyncInfo.Run((c) => action(c));
        }

        public object StartTimer(EventHandler<object> tickHandler, TimeSpan tickSpan, bool uiThread)
        {
            if (uiThread)
            {
                if (tickSpan.Ticks == 0)
                {
                    _uiDispatcher.BeginInvoke(() => tickHandler(null, null));
                    return null;
                }
                else
                {

                    TaskCompletionSource<DispatcherTimer> completionSource = new TaskCompletionSource<DispatcherTimer>();
                    _uiDispatcher.BeginInvoke(() =>
                    {
                        DispatcherTimer dt = new DispatcherTimer();
                        dt.Tick += (sender, args) => tickHandler(sender, args);
                        dt.Interval = tickSpan;
                        dt.Start();
                        completionSource.SetResult(dt);
                    });
                    return completionSource.Task;
                }
            }
            else
            {
                return ThreadPoolTimer.CreatePeriodicTimer((timer) => tickHandler(this, timer), tickSpan);
            }
        }

        public void RestartTimer(object tickHandle)
        {
            if (tickHandle is DispatcherTimer)
            {
                ((DispatcherTimer)tickHandle).Start();
            }
            else if (tickHandle is Task<DispatcherTimer>)
            {
                _uiDispatcher.BeginInvoke(async () =>
                {
                    var timer = await (Task<DispatcherTimer>)tickHandle;
                    timer.Start();
                });
            }
            else if (tickHandle is ThreadPoolTimer)
            {
                throw new NotImplementedException();
            }
        }


        public void StartThreadPoolTimer(Func<object, Task> action, TimeSpan timer)
        {
            ThreadPoolTimer.CreateTimer(async (obj) => await action(obj), timer);
        }

        public bool IsOnMeteredConnection { get; set; }
        public bool IsNearingDataLimit { get; set; }

        public Task<byte[]> DownloadWithProgress(string uri, Action<int> progress, CancellationToken cancelToken)
        {
            TaskCompletionSource<byte[]> taskCompletion = new TaskCompletionSource<byte[]>();
            WebClient client = new WebClient();
            int cancelCount = 0;
            client.AllowReadStreamBuffering = true;
            client.DownloadProgressChanged += (sender, args) =>
            {
                if (cancelToken.IsCancellationRequested)
                {
                    client.CancelAsync();
                }
                else
                {
                    progress(args.ProgressPercentage);
                }
            };

            client.OpenReadCompleted += (sender, args) =>
            {
                if (args.Cancelled)
                {
                    if (cancelCount++ < 5 && !cancelToken.IsCancellationRequested)
                        client.OpenReadAsync(new Uri(uri));
                    else
                        taskCompletion.SetCanceled();
                }
                else if (args.Error != null)
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        taskCompletion.SetCanceled();
                    }
                    else
                    {
                        taskCompletion.SetException(args.Error);
                    }
                }
                else
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        taskCompletion.SetCanceled();
                    }
                    else
                    {
                        var result = new byte[args.Result.Length];
                        args.Result.Read(result, 0, (int)args.Result.Length);
                        taskCompletion.SetResult(result);
                    }
                }
            };
            client.OpenReadAsync(new Uri(uri));
            return taskCompletion.Task;
        }


        public Task<byte[]> ResizeImage(byte[] data, int maxWidth, int maxHeight)
        {
            return CropPicture(data, new Size(maxWidth, maxHeight));
        }

        private static async Task<byte[]> CropPicture(byte[] data, Size desiredSize)
        {
            using (var source = new BufferImageSource(data.AsBuffer()))
            {
                var info = await source.GetInfoAsync();

                if (info.ImageSize.Width * info.ImageSize.Height > (desiredSize.Height * desiredSize.Width))
                {
                    var resizeConfiguration = new AutoResizeConfiguration(5 * 1024 * 1024, desiredSize,
                        new Size(0, 0), AutoResizeMode.Automatic, 0, ColorSpace.Yuv420);

                    return (await Nokia.Graphics.Imaging.JpegTools.AutoResizeAsync(source.Buffer, resizeConfiguration)).ToArray();
                }
                else
                {
                    return data;
                }
            }
            
        }

        private static Rect? GetCropArea(Size imageSize, Size desiredSize)
        {
            // how big is the picture compared to the phone?
            var widthRatio = desiredSize.Width / imageSize.Width;
            var heightRatio = desiredSize.Height / imageSize.Height;

            // the ratio is the same, no need to crop it
            if (widthRatio == heightRatio) return null;

            double cropWidth;
            double cropheight;
            if (widthRatio < heightRatio)
            {
                cropheight = imageSize.Height;
                cropWidth = desiredSize.Width / heightRatio;
            }
            else
            {
                cropheight = desiredSize.Height / widthRatio;
                cropWidth = imageSize.Width;
            }

            int left = (int)(imageSize.Width - cropWidth) / 2;
            int top = (int)(imageSize.Height - cropheight) / 2;

            var rect = new Windows.Foundation.Rect(left, top, cropWidth, cropheight);
            return rect;
        }

        public static Task<HttpWebResponse> GetResponseAsync(HttpWebRequest request)
        {
            var taskComplete = new TaskCompletionSource<HttpWebResponse>();
            request.BeginGetResponse(asyncResponse =>
            {
                try
                {
                    HttpWebRequest responseRequest = (HttpWebRequest)asyncResponse.AsyncState;
                    HttpWebResponse someResponse = (HttpWebResponse)responseRequest.EndGetResponse(asyncResponse);
                    taskComplete.TrySetResult(someResponse);
                }
                catch (Exception ex)
                {
                    taskComplete.TrySetException(ex);
                }
            }, request);
            return taskComplete.Task;
        }

        public static Task<Stream> GetRequestStreamAsync(HttpWebRequest request)
        {
            var taskComplete = new TaskCompletionSource<Stream>();
            request.BeginGetRequestStream(asyncResponse =>
            {
                try
                {
                    HttpWebRequest responseRequest = (HttpWebRequest)asyncResponse.AsyncState;
                    Stream someResponse = (Stream)responseRequest.EndGetRequestStream(asyncResponse);
                    taskComplete.TrySetResult(someResponse);
                }
                catch (Exception ex)
                {
                    taskComplete.TrySetException(ex);
                }
            }, request);
            return taskComplete.Task;
        }

        private async Task<string> SendGet(string uri, bool hasRetried)
        {
            HttpWebResponse getResult = null;
            bool needsRetry = false;
            try
            {
                HttpWebRequest request = HttpWebRequest.CreateHttp(uri);
                request.AllowReadStreamBuffering = true;
                request.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.UtcNow.ToString();
                request.Method = "GET";
                request.UserAgent = "Baconography_Windows_Phone_8_Client/1.0";
                var cookieContainer = new CookieContainer();
                request.CookieContainer = cookieContainer;

                getResult = await GetResponseAsync(request);
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.RequestCanceled)
                {
                    needsRetry = true;
                }
                else
                    throw;
            }

            if (needsRetry)
            {
                return await SendGet(uri, true);
            }

            if (getResult != null && getResult.StatusCode == HttpStatusCode.OK)
            {
                try
                {
                    return await (new StreamReader(getResult.GetResponseStream()).ReadToEndAsync());
                }
                catch (Exception ex)
                {
                    if (!hasRetried)
                        needsRetry = true;
                    else
                        throw ex;
                }
                if (needsRetry)
                    return await SendGet(uri, true);
                else
                    return null;
            }
            else if (!hasRetried)
            {
                int networkDownRetries = 0;
                while (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() && networkDownRetries < 10)
                {
                    networkDownRetries++;
                    await Task.Delay(1000);
                }

                return await SendGet(uri, true);
            }
            else
            {
                throw new Exception(getResult.StatusCode.ToString());
            }
        }
        public Task<string> SendGet(string uri)
        {
            return SendGet(uri, false);
        }

        public void ShowMessage(string title, string text)
        {
            RadMessageBox.Show(title, MessageBoxButtons.OK, text);
        }

        private static bool LowPriorityNetworkOkImpl()
        {
            if (DeviceNetworkInformation.IsNetworkAvailable && DeviceNetworkInformation.IsWiFiEnabled)
                return true;

            var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
            var connectionCost = connectionProfile.GetConnectionCost();
            var connectionCostType = connectionCost.NetworkCostType;
            if (connectionCostType != NetworkCostType.Unrestricted && connectionCostType != NetworkCostType.Unknown)
                return false;

            var interfaces = new NetworkInterfaceList();
            var targetInterface = interfaces.FirstOrDefault(net => net.InterfaceState == ConnectState.Connected);
            if (targetInterface == null)
                return false;

            if (targetInterface.Bandwidth < 1024 && targetInterface.Bandwidth > 0) //less then 1 meg means its a pretty shitty connection
                return false;

            var interfaceType = targetInterface.InterfaceSubtype;
            if (!(interfaceType == NetworkInterfaceSubType.Cellular_HSPA || interfaceType == NetworkInterfaceSubType.Cellular_LTE ||
                interfaceType == NetworkInterfaceSubType.Desktop_PassThru || interfaceType == NetworkInterfaceSubType.Cellular_EHRPD ||
                interfaceType == NetworkInterfaceSubType.WiFi || interfaceType == NetworkInterfaceSubType.Unknown))
                return false;

            return !(connectionCost.ApproachingDataLimit || connectionCost.OverDataLimit || connectionCost.Roaming);
        }

        private static bool IsHighPriorityNetworkOkImpl()
        {
            if (DeviceNetworkInformation.IsNetworkAvailable && DeviceNetworkInformation.IsWiFiEnabled)
                return true;

            var connectionProfile = NetworkInformation.GetInternetConnectionProfile();

            return DeviceNetworkInformation.IsNetworkAvailable && !connectionProfile.GetConnectionCost().OverDataLimit;
        }

        Lazy<bool> _lowPriorityNetworkOk;
        public bool IsLowPriorityNetworkOk { get { return _lowPriorityNetworkOk.Value; } }


        Lazy<bool> _highPriorityNetworkOk;
        public bool IsHighPriorityNetworkOk { get { return _highPriorityNetworkOk.Value; } }


        public Stream ResizeImage(Stream source, int maxWidth, int maxHeight)
        {
            return new NokiaResizeStream(source, maxWidth, maxHeight);
        }
        private class NokiaResizeStream : Stream
        {
            public NokiaResizeStream(Stream sourceStream, int maxWidth, int maxHeight)
            {
                
                _innerStream = new Lazy<Stream>(() =>
                    {
                        var desiredSize = new Size(maxWidth, maxHeight);
                        using (var dataWriter = new DataWriter(sourceStream.AsOutputStream()))
                        {
                            var resize = Nokia.Graphics.Imaging.JpegTools.AutoResizeAsync(dataWriter.DetachBuffer(),
                                new Nokia.Graphics.Imaging.AutoResizeConfiguration(5 * 1024 * 1024, desiredSize, desiredSize, Nokia.Graphics.Imaging.AutoResizeMode.PrioritizeHighEncodingQuality, 1.0, Nokia.Graphics.Imaging.ColorSpace.Undefined)).AsTask();

                            resize.Wait();
                            return resize.Result.AsStream();
                        }
                    });
            }


            protected override void Dispose(bool disposing)
            {
                if (disposing && _innerStream.IsValueCreated)
                {
                    _innerStream.Value.Dispose();
                }

                _innerStream = null;

                base.Dispose(disposing);
            }

            Lazy<Stream> _innerStream;
            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanSeek
            {
                get { return true; }
            }

            public override bool CanWrite
            {
                get { return false; }
            }

            public override void Flush()
            {
                _innerStream.Value.Flush();
            }

            public override long Length
            {
                get { return _innerStream.Value.Length; }
            }

            public override long Position
            {
                get
                {
                    return _innerStream.Value.Position;
                }
                set
                {
                    _innerStream.Value.Position = value;
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _innerStream.Value.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _innerStream.Value.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _innerStream.Value.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }   
        }

		List<WeakReference<ProgressIndicator>> _activeProgressIdicators = new List<WeakReference<ProgressIndicator>>();

		public void HideProgress ()
		{
			_uiDispatcher.BeginInvoke(() =>
				{
					if(SystemTray.ProgressIndicator == null)
					{
						SystemTray.ProgressIndicator = new ProgressIndicator();
						_activeProgressIdicators.Add(new WeakReference<ProgressIndicator>(SystemTray.ProgressIndicator));
					}

					_activeProgressIdicators.RemoveAll(weak =>
						{
							ProgressIndicator indicator;
							if(weak.TryGetTarget(out indicator))
							{
								indicator.IsVisible = false;
								return false;
							}
							else
								return true;
						});

				});
		}

		public void ShowProgress (string notificationText, double progressPercent)
		{
			_uiDispatcher.BeginInvoke(() =>
				{
					if(SystemTray.ProgressIndicator == null)
					{
						SystemTray.ProgressIndicator = new ProgressIndicator();
						_activeProgressIdicators.Add(new WeakReference<ProgressIndicator>(SystemTray.ProgressIndicator));
					}
					SystemTray.ProgressIndicator.IsVisible = true;
					SystemTray.ProgressIndicator.IsIndeterminate = progressPercent <= 0.01;
					SystemTray.ProgressIndicator.Text = notificationText;
					
					if(!SystemTray.ProgressIndicator.IsIndeterminate)
						SystemTray.ProgressIndicator.Value = progressPercent;
				});
		}
	}
}
