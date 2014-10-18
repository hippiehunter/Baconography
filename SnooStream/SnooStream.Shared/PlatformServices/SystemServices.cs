using GalaSoft.MvvmLight;
using Nokia.Graphics.Imaging;
using Nokia.InteropServices.WindowsRuntime;
using SnooStream.Common;
using SnooStream.Services;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.Connectivity;
using Windows.Security.Authentication.Web;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
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

#if WINDOWS_PHONE_APP
		List<WeakReference<StatusBarProgressIndicator>> _activeProgressIdicators = new List<WeakReference<StatusBarProgressIndicator>>();
#endif
		public void HideProgress()
		{
#if WINDOWS_PHONE_APP
			QueueNonCriticalUI(async () =>
			{
				var progressIndicator = StatusBar.GetForCurrentView().ProgressIndicator;
				await progressIndicator.HideAsync();
			});
#endif
		}

		public void ShowProgress(string notificationText, double? progressPercent)
		{
#if WINDOWS_PHONE_APP
			QueueNonCriticalUI(async () =>
			{
				var progressIndicator = StatusBar.GetForCurrentView().ProgressIndicator;
				progressIndicator.ProgressValue = progressPercent;
				progressIndicator.Text = notificationText;
				await progressIndicator.ShowAsync();
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
			while (!SnooStreamViewModel.UIContextCancellationToken.IsCancellationRequested)
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
			return new ImageLoader(this, uri, progress, cancelToken, errorHandler);
		}

		private class ImageLoader : ObservableObject, IImageLoader
		{
			private bool _isGif;
			private string _url;
			GifRenderer.GifPayload _gifPayload;
			Action<int> _progress;
			CancellationToken _cancelToken;
			Action<Exception> _errorHandler;

			public ImageLoader(ISystemServices systemServices, string uri, Action<int> progress, CancellationToken cancelToken, Action<Exception> errorHandler)
			{
				_isGif = true;
				_url = uri;
				_progress = progress;
				_cancelToken = cancelToken;
				_errorHandler = errorHandler;

				_forceLoadTask = new Lazy<Task>(() =>
				{
					return systemServices.DownloadWithProgress(_url, _progress, _cancelToken);
				});
			}


			public object ImageData
			{
				get
				{
					if (_isGif && _gifPayload == null)
					{
						GetPayload().ContinueWith((tsk) =>
							{
								if (tsk.Status == TaskStatus.RanToCompletion && tsk.IsCompleted)
								{
									_gifPayload = tsk.Result;
									Loaded = true;
									RaisePropertyChanged("ImageData");
									_gifPayload = null;
								}
								else
								{
									if (_errorHandler != null)
										_errorHandler(tsk.Exception);
								}

							}, TaskScheduler.FromCurrentSynchronizationContext());
						return null;
					}
					else if (_gifPayload != null)
					{
						var tmpPayload = _gifPayload;
						_gifPayload = null;
						return tmpPayload;
					}

					else
						return new GifRenderer.GifPayload { url = _url };
				}
			}

			private async Task<GifRenderer.GifPayload> GetPayload()
			{
				HttpClient client = new HttpClient();
				var response = await client.GetAsync(new Uri(_url), HttpCompletionOption.ResponseHeadersRead);
				var responseStream = await response.Content.ReadAsInputStreamAsync();
				var initialBuffer = await responseStream.ReadAsync(new Windows.Storage.Streams.Buffer(4096), 4096, InputStreamOptions.ReadAhead);
				if (initialBuffer.Length == 0)
					throw new Exception("failed to read initial bytes of image");

				var bufferBytes = new byte[initialBuffer.Length];
				initialBuffer.CopyTo(bufferBytes);

				_isGif = bufferBytes[0] == 0x47 && // G
						 bufferBytes[1] == 0x49 && // I
						 bufferBytes[2] == 0x46 && // F
						 bufferBytes[3] == 0x38 && // 8
						 (bufferBytes[4] == 0x39 || bufferBytes[4] == 0x37) && // 9 or 7
						 bufferBytes[5] == 0x61;   // a


				return new GifRenderer.GifPayload { initialData = bufferBytes.ToList(), inputStream = responseStream, url = _url };
			}

			public bool Loaded { get; set; }
			Lazy<Task> _forceLoadTask;
			public Task ForceLoad(int timeout)
			{
				//TODO let this timeout and trigger the cancel task
				return _forceLoadTask.Value;
			}
		}

		public async void RunUIAsync(Func<Task> action)
		{
			try
			{
				if (ViewModelBase.IsInDesignModeStatic)
				{
					await action();
				}
				else
					await (await _uiDispatcher).RunAsync(CoreDispatcherPriority.Normal, async () => await action());
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
			}
		}

		public async void RunUIIdleAsync(Func<Task> action)
		{
			await (await _uiDispatcher).RunIdleAsync(async (dispArgs) => await action());
		}


		public ObservableCollection<T> MakeIncrementalLoadCollection<T>(IIncrementalCollectionLoader<T> loader, int loadIncrement = 5, int auxiliaryTimeout = 2500)
		{
			return new BufferedAuxiliaryIncrementalLoadCollection<T>(loader, loadIncrement, auxiliaryTimeout);
		}


		internal class WrappedCollectionViewSource : IWrappedCollectionViewSource
		{
			internal static bool _dataBinding = false;
			private readonly CollectionViewSource _source = new CollectionViewSource();
			private readonly WrappedView _view;

			public WrappedCollectionViewSource(object source)
			{
				_view = new WrappedView(_source);
				Source = source;
				_source.View.CurrentChanging += View_CurrentChanging;
				_source.View.CurrentChanged += View_CurrentChanged;
			}

			void View_CurrentChanged(object sender, object e)
			{
				
			}

			void View_CurrentChanging(object sender, CurrentChangingEventArgs e)
			{
				if (_dataBinding)
					e.Cancel = true;
			}

			public object Source
			{
				get { return _source.Source; }
				set { _source.Source = value; }
			}

			public object UnderlyingSource
			{
				get { return _source; }
			}

			public IWrappedCollectionView View
			{
				get
				{
					if (_source.View == null)
						return null;
					return _view;
				}
			}

			private class WrappedView : IWrappedCollectionView
			{
				private readonly CollectionViewSource _source;

				public WrappedView(CollectionViewSource source)
				{
					_source = source;
				}

				public bool MoveCurrentTo(object item)
				{
					return _source.View.MoveCurrentTo(item);
				}

				public bool MoveCurrentToPosition(int position)
				{
					return _source.View.MoveCurrentToPosition(position);
				}

				public bool IsCurrentAfterLast
				{
					get { return _source.View.IsCurrentAfterLast; }
				}

				public bool MoveCurrentToFirst()
				{
					return _source.View.MoveCurrentToFirst();
				}

				public bool IsCurrentBeforeFirst
				{
					get { return _source.View.IsCurrentBeforeFirst; }
				}

				public bool MoveCurrentToLast()
				{
					return _source.View.MoveCurrentToLast();
				}

				public bool MoveCurrentToNext()
				{
					return _source.View.MoveCurrentToNext();
				}

				public bool MoveCurrentToPrevious()
				{
					return _source.View.MoveCurrentToPrevious();
				}

				public object CurrentItem
				{
					get { return _source.View.CurrentItem; }
				}

				public int CurrentPosition
				{
					get { return _source.View.CurrentPosition; }
				}
			}
		}

		public IWrappedCollectionViewSource MakeCollectionViewSource(object source)
		{
			return new WrappedCollectionViewSource(source);
		}



		public void ShowOAuthBroker()
		{
			String RedditURL = string.Format("https://ssl.reddit.com/api/v1/authorize?client_id={0}&response_type={1}&state={2}&redirect_uri={3}&duration={4}&scope={5}",
				"3m9rQtBinOg_rA", "code", "something", "http://www.google.com", "permanent", "modposts,identity,edit,flair,history,modconfig,modflair,modlog,modposts,modwiki,mysubreddits,privatemessages,read,report,save,submit,subscribe,vote,wikiedit,wikiread");

			System.Uri StartUri = new Uri(RedditURL);
			System.Uri EndUri = new Uri("http://www.google.com");
#if WINDOWS_PHONE_APP
			WebAuthenticationBroker.AuthenticateAndContinue(StartUri, EndUri, null, WebAuthenticationOptions.None);
#endif
		}
	}
}
