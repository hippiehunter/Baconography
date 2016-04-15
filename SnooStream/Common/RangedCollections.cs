using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Data;

namespace SnooStream.Common
{
    public class RangeCollection : RangedCollectionBase
    {
        public RangeCollection()
        {
        }

        public RangeCollection(IEnumerable<object> items) : base(items)
        {

        }
        public override bool HasMoreItems
        {
            get
            {
                return false;
            }
        }

        public override IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class AsyncRangedCollectionBase : RangedCollectionBase
    {
        protected bool HasLoaded = false;
        protected bool IsLoading = false;
        protected void AddRange<T>(IEnumerable<T> items) where T : class
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public override bool HasMoreItems
        {
            get
            {
                return !IsLoading;
            }
        }

        protected abstract Task<uint> LoadItemsAsync(uint count);

        private async Task<LoadMoreItemsResult> LoadItemsAsyncWrapper(uint count)
        {
            IsLoading = true;
            try
            {
                return new LoadMoreItemsResult { Count = await LoadItemsAsync(count) };
            }
            finally
            {
                IsLoading = false;
            }
        }

        public sealed override IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            HasLoaded = true;
            return LoadItemsAsyncWrapper(count).AsAsyncOperation<LoadMoreItemsResult>();
        }
    }

    public abstract class LoadItemCollectionBase : AsyncRangedCollectionBase
    {
        public override bool HasMoreItems
        {
            get
            {
                return !IsLoading && !this.OfType<LoadViewModel>().Any();
            }
        }

        protected virtual bool IsMergable { get { return false; } }
        protected abstract Task Refresh(IProgress<float> progress, CancellationToken token);
        protected abstract Task LoadInitial(IProgress<float> progress, CancellationToken token);
        protected abstract Task LoadAdditional(IProgress<float> progress, CancellationToken token);

        public void ClearState()
        {
            HasLoaded = false;
            Clear();
        }

        protected override Task<uint> LoadItemsAsync(uint count)
        {
            //Load Additional
            if (Count > 0)
            {
                var loadItem = new LoadViewModel { LoadAction = (progress, token) => LoadAdditional(progress, token), IsCritical = false, Kind = LoadKind.Collection };
                Add(loadItem);
                return LoadItem(loadItem);
            }
            else //Load fresh
            {
                var loadItem = new LoadViewModel { LoadAction = (progress, token) => LoadInitial(progress, token), IsCritical = true, Kind = LoadKind.Collection };
                Add(loadItem);
                return LoadItem(loadItem);
            }
        }

        public async Task Refresh()
        {
            var currentLoad = this.OfType<LoadViewModel>().FirstOrDefault();
            if (currentLoad != null)
            {
                currentLoad.Cancel();
                this.Remove(currentLoad);
            }

            IsLoading = true;
            try
            {
                if(!IsMergable)
                    Clear();
                    
                var loadItem = new LoadViewModel { LoadAction = (progress, token) => Refresh(progress, token), IsCritical = true, Kind = LoadKind.Collection };
                Insert(0, loadItem);
                await LoadItem(loadItem);
            }
            finally
            {
                HasLoaded = true;
                IsLoading = false;
            }
        }

        protected async Task<uint> LoadItem(LoadViewModel loadItem)
        {
            var itemCount = Count - 1;
            await loadItem.LoadAsync();
            //this needs to get attached into the retry for the load item somehow
            if (loadItem.State == LoadState.Loaded)
            {
                //now that the load is finished the load item should be removed from the list
                Remove(loadItem);
            }
            return (uint)(Count - itemCount);
        }
    }

    //this  almost works but not quite
    public class RangedSegmentedCollection : RangedCollectionBase
    {
        public List<Tuple<object, RangedCollectionBase>> SingleLoadCollections { get; set; }
        public RangedCollectionBase LoadMoreCollection { get; set; }
        object LoadMoreHeader;
        RangeCollection _collectionGroups;

        public RangedSegmentedCollection(IEnumerable<Tuple<object, RangedCollectionBase>> items, RangedCollectionBase loadMoreItem, object loadMoreItemHeader)
        {
            SingleLoadCollections = items.ToList();
            LoadMoreCollection = loadMoreItem;
            foreach (var item in CollectionGroups)
                Add(item);
        }

        public override IObservableVector<object> CollectionGroups
        {
            get
            {
                if (_collectionGroups == null)
                {
                    _collectionGroups = new RangeCollection(SingleLoadCollections.Select(col => new SegmentedCollectionViewGroup { GroupItems = col.Item2, Group = col.Item1 }));
                    _collectionGroups.Add(new SegmentedCollectionViewGroup { GroupItems = LoadMoreCollection, Group = LoadMoreHeader });
                }
                return _collectionGroups;
            }
        }

        public override bool HasMoreItems
        {
            get
            {
                return LoadMoreCollection.HasMoreItems || SingleLoadCollections.Any(collection => collection.Item2.HasMoreItems);
            }
        }

        public override IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            var firstSingleCollection = SingleLoadCollections.FirstOrDefault(collection => collection.Item2.HasMoreItems);
            if (firstSingleCollection != null)
                return firstSingleCollection.Item2.LoadMoreItemsAsync(count);
            else
                return LoadMoreCollection.LoadMoreItemsAsync(count);
        }

        class SegmentedCollectionViewGroup : ICollectionViewGroup
        {
            public object Group { get; set; }
            public IObservableVector<object> GroupItems { get; set; }
        }
    }



}
