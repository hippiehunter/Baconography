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
		bool _locked;
		int _loadIncrement;
		int _auxiliaryTimeout;
		public BufferedAuxiliaryIncrementalLoadCollection(IIncrementalCollectionLoader<T> loader, int loadIncrement = 5, int auxiliaryTimeout = 2500)
		{
			_loader = loader;
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
					var items = await _loader.LoadMore();
					_unloadedBuffer.AddRange(items);
				}

				if (_unloadedBuffer.Count > 0)
				{
					var targetItems = _unloadedBuffer.Take(_loadIncrement).ToList();
					_unloadedBuffer = _unloadedBuffer.Skip(_loadIncrement).ToList();
					await _loader.AuxiliaryItemLoader(targetItems, _auxiliaryTimeout);
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

		public void MaybeRefresh()
		{
			if (_loader.IsStale)
			{
				Refresh(false);
			}
		}

		public async void Refresh(bool onlyNew)
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
				await _loader.Refresh(this, onlyNew);
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
}
