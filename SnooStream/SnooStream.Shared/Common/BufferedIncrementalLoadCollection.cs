using SnooStream.Services;
using SnooStream.ViewModel;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace SnooStream.Common
{
	public class BufferedAuxiliaryIncrementalLoadCollection<T> : ObservableCollection<T>, ISupportIncrementalLoading, IRefreshable
	{
		IIncrementalCollectionLoader<T> _loader;
		List<T> _unloadedBuffer = new List<T>();
        HashSet<string> _uniqueItems = new HashSet<string>();
		bool _locked;
		int _loadIncrement;
		int _auxiliaryTimeout;
		public BufferedAuxiliaryIncrementalLoadCollection(IIncrementalCollectionLoader<T> loader, int loadIncrement = 20, int auxiliaryTimeout = 2500)
		{
			_loader = loader;
			loader.Attach(this);
			_locked = false;
			_loadIncrement = loadIncrement;
			_auxiliaryTimeout = auxiliaryTimeout;
		}

		public bool HasMoreItems
		{
			get { return _loader.HasMore(); }
		}

		public Windows.Foundation.IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
		{
			return LoadMoreItemsAsyncImpl(count).AsAsyncOperation();
		}

		private async Task<LoadMoreItemsResult> LoadMoreItemsAsyncImpl(uint count)
		{
			lock (this)
			{
				if (_locked)
					return new LoadMoreItemsResult { Count = 0 };
				else
					_locked = true;
			}
			try
			{
				if (_unloadedBuffer.Count == 0)
				{
					await SnooStreamViewModel.NotificationService.Report(string.Format("loading {0}s", _loader.NameForStatus),
						async () =>
						{
							var items = await _loader.LoadMore();
                            _unloadedBuffer.AddRange(items);
						});
				}

				if (_unloadedBuffer.Count > 0)
				{
					var targetItems = _unloadedBuffer.Take(_loadIncrement).ToList();
					_unloadedBuffer = _unloadedBuffer.Skip(_loadIncrement).ToList();
					var task = _loader.AuxiliaryItemLoader(targetItems, _auxiliaryTimeout);
                    var uniqueLoader = _loader as IUniqueIncrementalCollectionLoader<T>;
					foreach (var item in targetItems)
					{
                        Add(item);
					}
					return new LoadMoreItemsResult { Count = (uint)targetItems.Count };
				}
				else
					return new LoadMoreItemsResult { Count = 0 };
				
			}
			finally
			{
				lock (this)
				{
					_locked = false;
				}
			}
		}

		public async Task MaybeRefresh()
		{
			if (_loader.IsStale)
			{
				await Refresh(false);
			}
		}

		public async Task Refresh(bool onlyNew)
		{
			lock (this)
			{
				if (_locked)
					return;
				else
					_locked = true;
			}
			try
			{
				await SnooStreamViewModel.NotificationService.Report(string.Format("refreshing {0}s", _loader.NameForStatus),
						async () =>
						{
                            _unloadedBuffer.Clear();
							await _loader.Refresh(this, onlyNew);
						});
			}
			finally
			{
				lock (this)
				{
					_locked = false;
				}
			}
		}
	}

    public class AttachedIncrementalLoadCollection<T> : ObservableCollection<T>, ISupportIncrementalLoading
    {
        List<ISupportIncrementalLoading> _sources = new List<ISupportIncrementalLoading>();

        public void AttachCollection(ISupportIncrementalLoading sourceCollection)
        {
            if (sourceCollection.HasMoreItems)
            {
                var task = sourceCollection.LoadMoreItemsAsync(25);
            }

            _sources.Add(sourceCollection);
        }

        public void RemoveCollection(ISupportIncrementalLoading sourceCollection)
        {
            _sources.Remove(sourceCollection);
        }


        public bool HasMoreItems
        {
            get { return _sources.Any(incr => incr.HasMoreItems); }
        }

        public Windows.Foundation.IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            List<Task<LoadMoreItemsResult>> resultTasks = new List<Task<LoadMoreItemsResult>>();
            foreach(var incr in _sources)
            {
                resultTasks.Add(incr.LoadMoreItemsAsync(count).AsTask());
            }

            return Task.WhenAll(resultTasks).ContinueWith((rslt) => rslt.Result
                .Aggregate(new LoadMoreItemsResult { Count = 0}, (seed, itemsRslt) => { seed.Count += itemsRslt.Count; return seed;}), TaskContinuationOptions.OnlyOnRanToCompletion)
                .AsAsyncOperation();
        }
    }
}
