using GalaSoft.MvvmLight;
using SnooSharp;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public class NotificationService : ViewModelBase
    {
        public string NotificationText { get; private set; }
        public double ProgressPercent { get; private set; }
        public bool ProgressActive { get; private set; }
		bool _isProcessing;
        class NotificationInfo
        {
            public string Text { get; set; }
            public int Progress { get; set; }
        }

        List<NotificationInfo> _notificationStack = new List<NotificationInfo>();

        private void AddNotificationInfo(NotificationInfo info)
        {
			if (!ViewModelBase.IsInDesignModeStatic)
			{
				lock (this)
				{
					_notificationStack.Add(info);
					ProgressActive = true;
				}

				if (!_isProcessing)
				{
					_isProcessing = true;
					Task.Run(() => ProcessProgress());
				}
			}
        }

		async void ProcessProgress ()
		{
			try
			{
				while(ProgressActive)
				{
					var notificationStack = new List<NotificationInfo>();
					lock(this)
					{
						notificationStack.AddRange(_notificationStack);
					}


					if(notificationStack.Count == 1)
					{
						NotificationText = notificationStack[0].Text;
						ProgressPercent = Math.Max(0.0, ((double)notificationStack[0].Progress) / 100.0);
					}
					else
					{
						NotificationText = string.Format("loading {0} items", notificationStack.Count);
						ProgressPercent = Math.Max(0.0, ((double)notificationStack.Sum(notification => notification.Progress) / notificationStack.Count) / 100.0);
					}

					SnooStreamViewModel.SystemServices.ShowProgress(NotificationText, ProgressPercent > 0 ? (double?)ProgressPercent : null);
					await Task.Delay(500);
				}
				SnooStreamViewModel.SystemServices.HideProgress();

			}
			finally
			{
				_isProcessing = false;
			}
		}

        private void FinishNotificationInfo(NotificationInfo info)
        {
            lock (this)
            {
				try
				{
					_notificationStack.Remove(info);
					if(_notificationStack.Count == 0)
						ProgressActive = false;
				}
				catch { }
            }
        }

        private void ReprocessForProgress()
        {

        }

        DateTime _lastErrorTime = new DateTime();

        private void MaybeShowError(string message)
        {
            if ((DateTime.Now - _lastErrorTime).TotalSeconds > 10)
            {
                SnooStreamViewModel.SystemServices.ShowMessage("Error", message);
                _lastErrorTime = DateTime.Now;
            }
        }

        private void ProcessFailure(Exception ex, string message)
        {
            if (!SnooStreamViewModel.SystemServices.IsHighPriorityNetworkOk)
            {
                MaybeShowError("Network unavailable at this time please try again later");
            }
            else if (ex is RedditNotFoundException)
            {
                MaybeShowError(message + " reported " + ex.Message);
            }
            else if (ex is RedditException)
            {
                MaybeShowError(message + " reported " + ex.Message);
            }
            else
            {
                //_logger.Error("failed in reporting: " + message, ex);
            }
        }

        public async Task Report(string message, Func<Task> operation)
        {
            var notificationInfo = new NotificationInfo { Text = message, Progress = -1 };
            try
            {
                AddNotificationInfo(notificationInfo);
                await operation();
            }
            catch (Exception ex)
            {
                ProcessFailure(ex, message);
            }
            finally
            {
                FinishNotificationInfo(notificationInfo);
            }
        }

        public async Task ReportWithProgress(string message, Func<Action<int>, Task> operation)
        {
            var notificationInfo = new NotificationInfo { Text = message, Progress = 0 };
            try
            {
                AddNotificationInfo(notificationInfo);
                
                await operation((progress) =>
                {
                    notificationInfo.Progress = progress;
                    ReprocessForProgress();
                });
            }
            catch (Exception ex)
            {
                ProcessFailure(ex, message);
            }
            finally
            {
                FinishNotificationInfo(notificationInfo);
            }
        }

        public async Task ModalReportWithCancelation(string message, Func<CancellationToken, Task> operation)
        {
            var notificationInfo = new NotificationInfo { Text = message, Progress = -1 };
            try
            {
                AddNotificationInfo(notificationInfo);
                CancellationTokenSource cancelationTokenSource = new CancellationTokenSource();
                var opTask = operation(cancelationTokenSource.Token);
                if (await Task.WhenAny(opTask, Task.Delay(2500, cancelationTokenSource.Token)) == opTask)
                {
                    // task completed within timeout
                    cancelationTokenSource.Cancel();
                }
                else
                {
                    // timeout logic
                    //show cancel dialog
                    await Task.WhenAny(SnooStreamViewModel.NavigationService.ShowPopup(new OperationCancellationViewModel(message, cancelationTokenSource), null, cancelationTokenSource.Token), opTask);
                    cancelationTokenSource.Cancel();
                }
            }
            catch (TaskCanceledException)
            {
				//_logger.Info("Task Canceled: " + message);
            }
            catch (Exception ex)
            {
                ProcessFailure(ex, message);
            }
            finally
            {
                FinishNotificationInfo(notificationInfo);
            }
        }
    }
}
