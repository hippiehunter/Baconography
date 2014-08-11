using System;
using System.Collections.Generic;
using System.IO;
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
        Task<string> SendGet(string uri);
        void StartThreadPoolTimer(Func<object, Task> action, TimeSpan timer);
        Task<byte[]> DownloadWithProgress(string uri, Action<int> progress, CancellationToken cancelToken);
		Task<IImageLoader> DownloadImageWithProgress(string uri, Action<int> progress, CancellationToken cancelToken, Action<Exception> errorHandler);
        Task<byte[]> ResizeImage(byte[] data, int maxWidth, int maxHeight);
        Stream ResizeImage(Stream source, int maxWidth, int maxHeight);
        void ShowMessage(string title, string text);
        bool IsLowPriorityNetworkOk { get; }
        bool IsHighPriorityNetworkOk { get; }
		void ShowProgress(string notificationText, double progressPercent);
		void HideProgress();
		void QueueNonCriticalUI(Action action);
	}

	public interface IImageLoader
	{
        IImageSource ImageSource { get; }
        bool Loaded { get;}
		Task ForceLoad { get;}
	}
    
    public interface IImageSource : IDisposable
    {
        object ImageSource { get; }
    }
}
