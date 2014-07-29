using GalaSoft.MvvmLight;
using Nokia.Graphics.Imaging;
using Nokia.InteropServices.WindowsRuntime;
using SnooStream.Services;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.Connectivity;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Web.Http;

namespace SnooStream.PlatformServices
{
    class SystemServices : ISystemServices
    {
        private Task<CoreDispatcher> _uiDispatcher;
		private TaskCompletionSource<CoreDispatcher> _uiDispatcherSource = new TaskCompletionSource<CoreDispatcher>();
        public SystemServices()
        {
			_uiDispatcher = _uiDispatcherSource.Task;
            NetworkInformation.NetworkStatusChanged += networkStatusChanged;
            networkStatusChanged(null);
        }

		public void FinishInitialization(CoreDispatcher uiDispatcher)
		{
			_uiDispatcherSource.SetResult(uiDispatcher);
		}

        private void networkStatusChanged(object sender)
        {
            _lowPriorityNetworkOk = new Lazy<bool>(LowPriorityNetworkOkImpl);
            _highPriorityNetworkOk = new Lazy<bool>(IsHighPriorityNetworkOkImpl);
        }

        public async void StopTimer(object tickHandle)
        {
            if (tickHandle is DispatcherTimer)
            {
                if (((DispatcherTimer)tickHandle).IsEnabled)
                    ((DispatcherTimer)tickHandle).Stop();
            }
            else if (tickHandle is Task<DispatcherTimer>)
            {
                await (await _uiDispatcher).RunAsync(CoreDispatcherPriority.Normal, async () =>
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
                    var asyncItem = _uiDispatcher.ContinueWith((dispatcher) => dispatcher.Result.RunAsync(CoreDispatcherPriority.Normal, () => tickHandler(null, null)));
                    return null;
                }
                else
                {

                    TaskCompletionSource<DispatcherTimer> completionSource = new TaskCompletionSource<DispatcherTimer>();
                    _uiDispatcher.ContinueWith((dispatcher) => dispatcher.Result.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        DispatcherTimer dt = new DispatcherTimer();
                        dt.Tick += (sender, args) => tickHandler(sender, args);
                        dt.Interval = tickSpan;
                        dt.Start();
                        completionSource.SetResult(dt);
                    }));
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
				var asyncItem = _uiDispatcher.ContinueWith((dispatcher) => dispatcher.Result.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    var timer = await (Task<DispatcherTimer>)tickHandle;
                    timer.Start();
                }));
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
            HttpClient client = new HttpClient();
            int cancelCount = 0;
            Action doGet = null;
            doGet = () =>
             {
                 var response = client.GetAsync(new Uri(uri));
                 response.Progress = (message, value) =>
                     {
                         if ((value.TotalBytesToReceive ?? 0) > 0)
                         {
                             var progressValue = (double)(value.TotalBytesToReceive ?? 0) / (double)value.BytesReceived;
                             progress((int)progressValue * 100);
                         }

                         if (cancelToken.IsCancellationRequested)
                         {
                             response.Cancel();
                         }
                     };

                 response.Completed = async (message, value) =>
                     {
                         switch (value)
                         {
                             case AsyncStatus.Completed:
                                 taskCompletion.TrySetResult((await message.GetResults().Content.ReadAsBufferAsync()).ToArray());
                                 break;
                             case AsyncStatus.Canceled:
                                 if (cancelCount++ > 5 || cancelToken.IsCancellationRequested)
                                     taskCompletion.TrySetCanceled();
                                 else
                                 {
                                     doGet();
                                 }
                                 break;
                             case AsyncStatus.Error:
                                 taskCompletion.TrySetException(message.ErrorCode);
                                 break;
                             default:
                                 break;

                         }

                     };
             };

            doGet();
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

            if (getResult != null && getResult.StatusCode == System.Net.HttpStatusCode.OK)
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
			var dialog = new MessageDialog(text, title);
			var asyncOp = dialog.ShowAsync(); //just continue it doesnt matter at this point anyway
        }

        private static bool LowPriorityNetworkOkImpl()
        {
            var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
			if (connectionProfile.GetNetworkConnectivityLevel() != NetworkConnectivityLevel.InternetAccess)
				return false;

            var connectionCost = connectionProfile.GetConnectionCost();
            var connectionCostType = connectionCost.NetworkCostType;
			var connectionStrength = connectionProfile.GetSignalBars() ?? 5;
            if (connectionCostType != NetworkCostType.Unrestricted && connectionCostType != NetworkCostType.Unknown)
                return false;

			if (connectionProfile.IsWwanConnectionProfile)
			{
				var connectionClass = connectionProfile.WwanConnectionProfileDetails.GetCurrentDataClass();
				switch (connectionClass)
				{
					case WwanDataClass.Hsdpa:
					case WwanDataClass.Hsupa:
					case WwanDataClass.LteAdvanced:
					case WwanDataClass.Umts:
						break;
					default:
						return false;
				}

				if (connectionStrength < 3)
					return false;
			}

            return !(connectionCost.ApproachingDataLimit || connectionCost.OverDataLimit || connectionCost.Roaming);
        }

        private static bool IsHighPriorityNetworkOkImpl()
        {
			var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
			if (connectionProfile.GetNetworkConnectivityLevel() != NetworkConnectivityLevel.InternetAccess)
				return false;

			var connectionCost = connectionProfile.GetConnectionCost();
			return !connectionCost.OverDataLimit;
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

#if WINDOWS_PHONE_APP
        List<WeakReference<StatusBarProgressIndicator>> _activeProgressIdicators = new List<WeakReference<StatusBarProgressIndicator>>();
#endif
		public void HideProgress ()
		{
#if WINDOWS_PHONE_APP
			QueueNonCriticalUI(() =>
			{
				var progressIndicator = StatusBar.GetForCurrentView().ProgressIndicator;
				progressIndicator.HideAsync();
			});
#endif
		}

		public void ShowProgress (string notificationText, double progressPercent)
		{
#if WINDOWS_PHONE_APP
			QueueNonCriticalUI(() =>
			{
				var progressIndicator = StatusBar.GetForCurrentView().ProgressIndicator;
				progressIndicator.ShowAsync();
			});
#endif
		}

		bool _isDrainingUIQueue = false;
		List<Action> _queuedNonCritical = new List<Action>();
		public void QueueNonCriticalUI(Action action)
		{
			lock (_queuedNonCritical)
			{
				_queuedNonCritical.Add(action);
				if (!_isDrainingUIQueue)
				{
					_isDrainingUIQueue = true;
					Task.Run(() => DrainNonCriticalUIQueue());
				}
			}
		}

		DateTime _lastDrained = DateTime.Now;
		private async void DrainNonCriticalUIQueue()
		{
			_lastDrained = DateTime.Now;
			while ((DateTime.Now - _lastDrained).TotalSeconds < 2 && !SnooStreamViewModel.UIContextCancellationToken.IsCancellationRequested)
			{
				bool needToRun = false;
				lock (_queuedNonCritical)
				{
					if (_queuedNonCritical.Count > 0)
					{
						needToRun = true;
					}
				}

				if (needToRun)
				{
					await (await _uiDispatcher).RunIdleAsync((args) =>
							{
								try
								{
									lock (_queuedNonCritical)
									{
										foreach (var queued in _queuedNonCritical)
										{
											queued();
										}
										_queuedNonCritical.Clear();
									}
								}
								catch { }
							});
				}
				await Task.Delay(100);
			}
		}


		public IImageLoader DownloadImageWithProgress(string uri, Action<int> progress, CancellationToken cancelToken, Action<Exception> errorHandler)
		{
			var imageLoader = new ImageLoader(uri, _uiDispatcher, errorHandler);
			imageLoader.InitialLoad(progress, cancelToken);
			return imageLoader;
		}

		class ImageLoader : ViewModelBase, IImageLoader
		{
			public ImageLoader(string url, Task<CoreDispatcher> dispatcher, Action<Exception> errorHandler)
			{
				_url = url;
				_dispatcher = dispatcher;
				_internalLoader = new WeakReference<ImageLoaderInternal>(null);
				_errorHandler = errorHandler;
			}

			public async void InitialLoad(Action<int> progress, CancellationToken cancelToken)
			{
				HttpClient client = new HttpClient();
				var response = await client.GetAsync(new Uri(_url), HttpCompletionOption.ResponseHeadersRead);

				var responseStream = (await response.Content.ReadAsInputStreamAsync()).AsStreamForRead();
				var initialBuffer = new byte[4096];
				var initialReadLength = await responseStream.ReadAsync(initialBuffer, 0, 4096);
				if (initialReadLength == 0)
					throw new Exception("failed to read initial bytes of image");

				var contentLengthHeader = response.Headers.ContainsKey("Content-Length") ? response.Headers["Content-Length"] : "-1";
				int contentLength = -1;
				int.TryParse(contentLengthHeader, out contentLength);

				if ((await _dispatcher).HasThreadAccess)
				{
					await InternalLoader.Init(initialBuffer, responseStream, _url, progress, cancelToken, contentLength, _errorHandler);
					RaisePropertyChanged("ImageSource");
				}
				else
				{
					await (await _dispatcher).RunAsync(CoreDispatcherPriority.High, async () =>
					{
						await InternalLoader.Init(initialBuffer, responseStream, _url, progress, cancelToken, contentLength, _errorHandler);
						RaisePropertyChanged("ImageSource");
					});
				}

				
			}
			private string _url;
			Action<Exception> _errorHandler;
			private Task<CoreDispatcher> _dispatcher;
			private WeakReference<ImageLoaderInternal> _internalLoader;
			private ImageLoaderInternal InternalLoader
			{
				get
				{
					ImageLoaderInternal internalLoader;
					if (!_internalLoader.TryGetTarget(out internalLoader) || internalLoader == null)
					{
						lock (this)
						{
							if (!_internalLoader.TryGetTarget(out internalLoader) || internalLoader == null)
							{
								internalLoader = new ImageLoaderInternal();
								_internalLoader.SetTarget(internalLoader);
								InitialLoad(null, CancellationToken.None);
							}
						}
					}
					return internalLoader;
				}
					
			}

			private class ImageLoaderInternal
			{
				public async Task Init(byte[] initialData, Stream responseStream, string url, Action<int> progress, CancellationToken cancelToken, int contentLength, Action<Exception> errorHandler)
				{
					_errorHandler = errorHandler;
					_cancelToken = new CancellationTokenSource();
					_contentLength = contentLength;
					_progress = progress;
					_responseStream = responseStream;
					_url = url;
					_isGif = CheckGif(initialData);
					_loadTask = new Lazy<Task>(() => Load());
					if (_isGif)
					{
						bool tryAfter = false;
						_memoryStream.Write(initialData, 0, initialData.Length);
						var readBytes = await responseStream.ReadAsync(initialData, 0, 4096);
						_memoryStream.Write(initialData, 0, readBytes);
						if (readBytes > 0)
						{
							readBytes = await responseStream.ReadAsync(initialData, 0, 4096);
							_memoryStream.Write(initialData, 0, readBytes);
						}
						try
						{
							_returnedPosition = 0;
							_gifRenderer = new GifRenderer.GifRenderer(GetMore);
						}
						catch(ArgumentException)
						{
							tryAfter = true;
						}
						catch(Exception ex)
						{
							_errorHandler(ex);
						}
						if(tryAfter)
						{
							try
							{
								await Load();
								_returnedPosition = 0;
								_gifRenderer = new GifRenderer.GifRenderer(GetMore);
							}
							catch (Exception ex)
							{
								_errorHandler(ex);
							}
						}

					}
					else
					{
						_responseStream.Dispose();
						_responseStream = null;
						_cancelCallback = cancelToken.Register(() =>
						{
							_cancelToken.Cancel();
							if (_bitmapImage != null)
							{
								_bitmapImage.UriSource = null;
								_bitmapImage = null;
							}
						});
					}
					_initialLoaded = true;
				}

				Action<Exception> _errorHandler;
				bool _initialLoaded = false;
				bool _isGif;
				GifRenderer.GifRenderer _gifRenderer;
				BitmapImage _bitmapImage;
				Action<int> _progress;
				Lazy<Task> _loadTask;
				int _contentLength;
				CancellationTokenSource _cancelToken;
				CancellationTokenRegistration _cancelCallback;
				bool _finished = false;
				long _returnedPosition = 0;
				MemoryStream _memoryStream = new MemoryStream();
				Stream _responseStream;
				string _url;

				private static bool CheckGif(byte[] data)
				{
					return
						data[0] == 0x47 && // G
						data[1] == 0x49 && // I
						data[2] == 0x46 && // F
						data[3] == 0x38 && // 8
						(data[4] == 0x39 || data[4] == 0x37) && // 9 or 7
						data[5] == 0x61;   // a
				}

				public byte[] GetMore()
				{
					//its over
					if (_memoryStream == null || (_finished && _returnedPosition == _memoryStream.Length))
						return null;

					if (_returnedPosition < _memoryStream.Length)
					{
						lock (_memoryStream)
						{
							var result = new byte[_memoryStream.Length - _returnedPosition];
							_memoryStream.Seek(_returnedPosition, SeekOrigin.Begin);
							_memoryStream.Read(result, 0, result.Length);
							_returnedPosition += result.Length;
							return result;
						}
					}
					else
						return new byte[0];
				}

				public async Task Load()
				{
					if (_responseStream != null)
					{
						while (!_initialLoaded)
						{
							await Task.Delay(100);
						}
						try
						{
							var buffer = new byte[4096];
							for (; ; )
							{
								_cancelToken.Token.ThrowIfCancellationRequested();
								var readBytes = await _responseStream.ReadAsync(buffer, 0, 4096);
								if (readBytes == 0)
									break;
								else
								{
									_cancelToken.Token.ThrowIfCancellationRequested();
									if (_memoryStream != null)
									{
										lock (_memoryStream)
										{
											_memoryStream.Seek(0, SeekOrigin.End);
											_memoryStream.Write(buffer, 0, readBytes);
										}
									}
									else
										return;
								}
							}
						}
						catch { }
						finally
						{
							_finished = true;
							try
							{
								_cancelCallback.Dispose();
								if (_responseStream != null)
								{
									_responseStream.Dispose();
									_responseStream = null;
								}
							}
							catch { }
						}
					}
				}


				public object ImageSource
				{
					get
					{
						if (!_initialLoaded)
							return null;
						else
						{
							if (_isGif)
							{
								if (_loadTask.Value != null && _gifRenderer != null)
									return _gifRenderer.ImageSource;
								else
									return null;
							}
							else
							{
								if (_bitmapImage == null)
								{
									lock (this)
									{
										if (_bitmapImage == null)
										{
											_bitmapImage = new BitmapImage();
											_bitmapImage.DownloadProgress += _bitmapImage_DownloadProgress;
											_bitmapImage.ImageOpened += _bitmapImage_ImageOpened;
											_bitmapImage.ImageFailed += _bitmapImage_ImageFailed;
											_bitmapImage.UriSource = new Uri(_url);
										}
									}
								}
								return _bitmapImage;
							}
						}
					}
				}

				void _bitmapImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
				{
					_cancelCallback.Dispose();
					var bitmapImage = sender as BitmapImage;
					if (bitmapImage != null)
					{
						_bitmapImage.DownloadProgress -= _bitmapImage_DownloadProgress;
						_bitmapImage.ImageOpened -= _bitmapImage_ImageOpened;
						_bitmapImage.ImageFailed -= _bitmapImage_ImageFailed;
					}
					_errorHandler(new Exception(e.ErrorMessage));
				}

				void _bitmapImage_ImageOpened(object sender, RoutedEventArgs e)
				{
					_finished = true;
					_cancelCallback.Dispose();
					_progress = null;
					var bitmapImage = sender as BitmapImage;
					if (bitmapImage != null)
					{
						_bitmapImage.DownloadProgress -= _bitmapImage_DownloadProgress;
						_bitmapImage.ImageOpened -= _bitmapImage_ImageOpened;
						_bitmapImage.ImageFailed -= _bitmapImage_ImageFailed;
					}
				}

				void _bitmapImage_DownloadProgress(object sender, DownloadProgressEventArgs e)
				{
					if (_progress != null)
					{
						_progress(e.Progress);
					}
				}

				~ImageLoaderInternal()
				{
					if (_gifRenderer != null)
					{
						_gifRenderer.Dispose();
						_gifRenderer = null;
					}

					if (_memoryStream != null)
						_memoryStream.Dispose();
					_memoryStream = null;

					if (_responseStream != null)
						_responseStream.Dispose();
					_responseStream = null;

					_cancelCallback.Dispose();
				}

				public bool Loaded
				{
					get
					{
						return _finished;
					}
				}
			}

			public object ImageSource
			{
				get 
				{
					return InternalLoader.ImageSource;
				}
			}


			public bool Loaded
			{
				get { return InternalLoader.Loaded; }
			}

			public Task ForceLoad
			{
				get { return InternalLoader.Load(); }
			}
		}
	}
}
