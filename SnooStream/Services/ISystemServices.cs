﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.Services
{
    public interface ISystemServices
    {
        object StartTimer(EventHandler<object> tickHandler, TimeSpan tickSpan, bool uiThread);
        void RestartTimer(object tickHandle);
        void StopTimer(object tickHandle);
        void RunAsync(Func<object, Task> action);
        void StartThreadPoolTimer(Func<object, Task> action, TimeSpan timer);
        Task<byte[]> DownloadWithProgress(string uri, Action<int> progress, CancellationToken cancelToken);
        Task<byte[]> ResizeImage(byte[] data, int maxWidth, int maxHeight);
        void ShowMessage(string title, string text);
        bool IsLowPriorityNetworkOk { get; }
        bool IsHighPriorityNetworkOk { get; }
    }
}
