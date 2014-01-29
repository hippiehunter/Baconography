﻿using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public class NotificationService : ViewModelBase
    {
        public string NotificationText { get; private set; }
        public float ProgressPercent { get; private set; }
        public bool ProgressActive { get; private set; }

        class NotificationInfo
        {
            public string Text { get; set; }
            public int Progress { get; set; }
        }

        List<NotificationInfo> _notificationStack = new List<NotificationInfo>();

        private void AddNotificationInfo(NotificationInfo info)
        {
            lock (this)
            {
                
            }
        }

        private void FinishNotificationInfo(NotificationInfo info)
        {
            lock (this)
            {

            }
        }

        private void ReprocessForProgress()
        {

        }

        public void Report(string message, Action operation)
        {
            var notificationInfo = new NotificationInfo { Text = message, Progress = -1};
            try
            {
                AddNotificationInfo(notificationInfo);
                operation();
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
                SnooStream.ViewModel.SnooStreamViewModel.SystemServices.ShowMessage("error", ex.ToString());
            }
            finally
            {
                FinishNotificationInfo(notificationInfo);
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
                Debug.WriteLine(ex);
                SnooStream.ViewModel.SnooStreamViewModel.SystemServices.ShowMessage("error", ex.ToString());
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
                
                await PriorityLoadQueue.QueueHelper(() =>
                    {
                        return operation((progress) =>
                        {
                            notificationInfo.Progress = progress;
                            ReprocessForProgress();
                        });
                    })();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                SnooStream.ViewModel.SnooStreamViewModel.SystemServices.ShowMessage("error", ex.ToString());
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
                if (await Task.WhenAny(opTask, Task.Delay(1500, cancelationTokenSource.Token)) == opTask)
                {
                    // task completed within timeout
                    cancelationTokenSource.Cancel();
                }
                else
                {
                    // timeout logic
                    //show cancel dialog
                    await opTask;
                }
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("task canceled");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                SnooStream.ViewModel.SnooStreamViewModel.SystemServices.ShowMessage("error", ex.ToString());
            }
            finally
            {
                FinishNotificationInfo(notificationInfo);
            }
        }
    }
}
