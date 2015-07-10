using GalaSoft.MvvmLight;
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
using Windows.ApplicationModel.DataTransfer;
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
			_lowPriorityNetworkOk = new Lazy<bool>(SnooStreamBackground.NetworkUtilities.LowPriorityNetworkOk);
			_highPriorityNetworkOk = new Lazy<bool>(SnooStreamBackground.NetworkUtilities.IsHighPriorityNetworkOk);
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

		public void ShowMessage(string title, string text)
		{
			var dialog = new MessageDialog(text, title);
			var asyncOp = dialog.ShowAsync(); //just continue it doesnt matter at this point anyway
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
								catch (Exception ex)
								{
									//_logger.Error("failed draining non critical UI Queue", ex);
								}
							});
				}
				await Task.Delay(100);
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
					await (await _uiDispatcher).RunAsync(CoreDispatcherPriority.Normal, async () =>
						{
							try
							{
								await action();
							}
							catch (Exception ex)
							{
								//_logger.Error("action failed in RunUIAsync", ex);
							}
						});
			}
			catch (Exception ex)
			{
				//_logger.Error("action failed in RunUIAsync", ex);
			}
		}

		public async void RunUIIdleAsync(Func<Task> action)
		{
			await (await _uiDispatcher).RunIdleAsync(async (dispArgs) => 
				{
					try
					{
						await action();
					}
					catch (Exception ex)
					{
						//_logger.Error("action failed in RunUIIdleAsync", ex);
					}
				});
		}


		public ObservableCollection<T> MakeIncrementalLoadCollection<T>(IIncrementalCollectionLoader<T> loader, int loadIncrement = 5, int auxiliaryTimeout = 2500, bool eager = false)
		{
			return new BufferedAuxiliaryIncrementalLoadCollection<T>(loader, loadIncrement, auxiliaryTimeout, eager);
		}

        public ObservableCollection<T> MakeFilteredIncrementalLoadCollection<T>(ObservableCollection<T> sourceCollection, Func<T, bool> filter) where T : class
        {
            return new FilteredIncrementalLoadCollection<T, BufferedAuxiliaryIncrementalLoadCollection<T>>(sourceCollection as BufferedAuxiliaryIncrementalLoadCollection<T>, filter);
        }

        public ObservableCollection<T> FilterAttachIncrementalLoadCollection<T, T2>(ObservableCollection<T2> incrementalSource, ObservableCollection<T> filteredCollection)
        {
            var result = (filteredCollection as AttachedIncrementalLoadCollection<T>) ?? new AttachedIncrementalLoadCollection<T>();
            if(incrementalSource is ISupportIncrementalLoading)
                result.AttachCollection(incrementalSource as ISupportIncrementalLoading);
            return result;
        }

        public void FilterDetachIncrementalLoadCollection<T, T2>(ObservableCollection<T> filteredCollection, ObservableCollection<T2> incrementalSource)
        {
            var attached = filteredCollection as AttachedIncrementalLoadCollection<T>;
            if(attached != null && incrementalSource is ISupportIncrementalLoading)
                attached.RemoveCollection(incrementalSource as ISupportIncrementalLoading);
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
#if WINDOWS_PHONE_APP
			String RedditURL = string.Format("https://ssl.reddit.com/api/v1/authorize.compact?client_id={0}&response_type={1}&state={2}&redirect_uri={3}&duration={4}&scope={5}",
				"3m9rQtBinOg_rA", "code", "something", "http://www.google.com", "permanent", "modposts,identity,edit,flair,history,modconfig,modflair,modlog,modposts,modwiki,mysubreddits,privatemessages,read,report,save,submit,subscribe,vote,wikiedit,wikiread");
#else
            String RedditURL = string.Format("https://ssl.reddit.com/api/v1/authorize?client_id={0}&response_type={1}&state={2}&redirect_uri={3}&duration={4}&scope={5}",
                "3m9rQtBinOg_rA", "code", "something", "http://www.google.com", "permanent", "modposts,identity,edit,flair,history,modconfig,modflair,modlog,modposts,modwiki,mysubreddits,privatemessages,read,report,save,submit,subscribe,vote,wikiedit,wikiread");
#endif

			System.Uri StartUri = new Uri(RedditURL);
			System.Uri EndUri = new Uri("http://www.google.com");
#if WINDOWS_PHONE_APP
			WebAuthenticationBroker.AuthenticateAndContinue(StartUri, EndUri, null, WebAuthenticationOptions.None);
#endif
		}

        public void ShareLink(string url, string title, string description)
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager,
                DataRequestedEventArgs>((sender, e) =>
                {
                    DataRequest request = e.Request;
                    request.Data.Properties.Title = title;
                    request.Data.Properties.Description = description;
                    request.Data.SetWebLink(new Uri(url));
                });

            Windows.ApplicationModel.DataTransfer.DataTransferManager.ShowShareUI();
        }


        public Task<T> RunAsyncIdle<T>(Func<T> action, CancellationToken cancelToken)
        {
            TaskCompletionSource<T> source = new TaskCompletionSource<T>();
            var poolTask = ThreadPool.RunAsync((op) =>
                {
                    try
                    {
                        if (cancelToken.IsCancellationRequested)
                        {
                            source.TrySetCanceled();
                            return;
                        }
                        var result = action();
                        if (cancelToken.IsCancellationRequested)
                        {
                            source.TrySetCanceled();
                        }
                        else
                        {
                            source.TrySetResult(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        source.TrySetException(ex);
                    }

                }, WorkItemPriority.Low, WorkItemOptions.None);
            return source.Task;
        }


        public Task<string> ImagePreviewFromUrl(string url, CancellationToken cancel)
        {
            return PlatformImageAcquisition.ImagePreviewFromUrl(url, cancel);
        }
    }
}
