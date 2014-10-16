using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
		void RunUIAsync(Func<Task> action);
		void RunUIIdleAsync(Func<Task> action);
        Task<string> SendGet(string uri);
        void StartThreadPoolTimer(Func<object, Task> action, TimeSpan timer);
        Task<byte[]> DownloadWithProgress(string uri, Action<int> progress, CancellationToken cancelToken);
		IImageLoader DownloadImageWithProgress(string uri, Action<int> progress, CancellationToken cancelToken, Action<Exception> errorHandler);
        Task<byte[]> ResizeImage(byte[] data, int maxWidth, int maxHeight);
        Stream ResizeImage(Stream source, int maxWidth, int maxHeight);
        void ShowMessage(string title, string text);
        bool IsLowPriorityNetworkOk { get; }
        bool IsHighPriorityNetworkOk { get; }
		void ShowProgress(string notificationText, double? progressPercent);
		void HideProgress();
		void QueueNonCriticalUI(Action action);
		ObservableCollection<T> MakeIncrementalLoadCollection<T>(IIncrementalCollectionLoader<T> loader, int loadIncrement = 20, int auxiliaryTimeout = 2500);
		IWrappedCollectionViewSource MakeCollectionViewSource(object source);
	}

	public interface IIncrementalCollectionLoader<T>
	{
		//Get 2nd part of two part loader, takes elements, and timeout in milliseconds, returns Task
		Task AuxiliaryItemLoader(IEnumerable<T> items, int timeout);
		bool IsStale { get; }
		bool HasMore();
		Task<IEnumerable<T>> LoadMore();
		Task Refresh(ObservableCollection<T> current, bool onlyNew);
		string NameForStatus { get; }
	}

	public interface IWrappedCollectionView
	{
		object CurrentItem { get; }
		int CurrentPosition { get; }

		bool MoveCurrentTo(object item);
		bool MoveCurrentToPosition(int position);
		bool IsCurrentAfterLast { get; }
		bool MoveCurrentToFirst();
		bool IsCurrentBeforeFirst { get; }
		bool MoveCurrentToLast();
		bool MoveCurrentToNext();
		bool MoveCurrentToPrevious();
	}

	public interface IWrappedCollectionViewSource
	{
		object UnderlyingSource { get; }
		IWrappedCollectionView View { get; }
		object Source { get; set; }
	}

	public interface IImageLoader
	{
        object ImageData { get; }
        bool Loaded { get;}

		/// <summary>
		/// force load with a timeout
		/// </summary>
		/// <param name="timeout">timeout in milliseconds before the image load is aborted</param>
		/// <returns></returns>
		Task ForceLoad(int timeout);
	}
}
