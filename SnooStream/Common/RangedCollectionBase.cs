using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;

namespace SnooStream.Common
{
    public abstract class RangedCollectionBase : ObservableCollection<object>, ICollectionView, IItemsRangeInfo
    {
        public RangedCollectionBase()
        {
            CollectionChanged += RangedCollectionBase_CollectionChanged;
        }

        public RangedCollectionBase(IEnumerable<object> items) : base(items)
        {
            CollectionChanged += RangedCollectionBase_CollectionChanged;
        }

        public IObservableVector<object> CollectionGroups
        {
            get
            {
                return null;
            }
        }

        public object CurrentItem { get { return CurrentPosition > -1 && CurrentPosition < Count ? this[CurrentPosition] : null; } set { CurrentPosition = IndexOf(value); } }
        private int _currentPosition = -1;
        public int CurrentPosition
        {
            get
            {
                return _currentPosition;
            }
            set
            {
                if (value != _currentPosition)
                {
                    _currentPosition = value;
                    RebindCurrent();
                }
            }
        }

        public abstract bool HasMoreItems { get; }
        public abstract IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count);

        public bool IsCurrentAfterLast
        {
            get
            {
                return CurrentPosition >= Count;
            }
        }

        public bool IsCurrentBeforeFirst
        {
            get
            {
                return CurrentPosition < 0;
            }
        }

        public bool Movable { get; internal set; } = true;
        public bool RangeMovable { get; internal set; } = true;

        public event EventHandler<object> CurrentChanged;
        public event CurrentChangingEventHandler CurrentChanging;
        public event VectorChangedEventHandler<object> VectorChanged;

        private void RangedCollectionBase_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var hasNewItems = e.NewItems != null && e.NewItems.Count > 0;
            if (VectorChanged != null)
            {
                try
                {
                    Movable = false;
                    VectorChanged(this, new VectorChangedEventArgs(e.Action, hasNewItems ? e.NewItems[0] : e.OldItems, hasNewItems ? e.NewStartingIndex : e.OldStartingIndex));
                }
                finally
                {
                    Movable = true;
                }
            }
        }

        public void Dispose()
        {
            CollectionChanged -= RangedCollectionBase_CollectionChanged;
        }

        public bool MoveCurrentTo(object item)
        {
            if (Movable)
            {
                CurrentItem = item;
                return item != null;
            }
            else
            {
                return item == CurrentItem;
            }
        }

        public bool MoveCurrentToFirst()
        {
            CurrentPosition = Count > 0 ? 0 : -1;
            return Count > 0;
        }

        public bool MoveCurrentToLast()
        {
            CurrentPosition = Count - 1;
            return Count > 0;
        }

        public bool MoveCurrentToNext()
        {
            CurrentPosition++;
            return CurrentPosition < Count;
        }

        public bool MoveCurrentToPosition(int index)
        {
            CurrentPosition = index;
            return index > 0 && index < Count;
        }

        public bool MoveCurrentToPrevious()
        {
            CurrentPosition--;
            return CurrentPosition > -1;
        }

        public void RangesChanged(ItemIndexRange visibleRange, IReadOnlyList<ItemIndexRange> trackedItems)
        {
            if(RangeMovable && Movable)
                _currentPosition = visibleRange.FirstIndex;
        }
        
        private sealed class VectorChangedEventArgs : IVectorChangedEventArgs
        {
            public VectorChangedEventArgs(NotifyCollectionChangedAction action, object item, int index)
            {
                switch (action)
                {
                    case NotifyCollectionChangedAction.Add:
                        CollectionChange = CollectionChange.ItemInserted;
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        CollectionChange = CollectionChange.ItemRemoved;
                        break;
                    case NotifyCollectionChangedAction.Move:
                    case NotifyCollectionChangedAction.Replace:
                        CollectionChange = CollectionChange.ItemChanged;
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        CollectionChange = CollectionChange.Reset;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("action");
                }
                Index = (uint)index;
                Item = item;
            }

            /// <summary>
            /// Gets the affected item.
            /// </summary>
            public object Item { get; private set; }

            /// <summary>
            /// Gets the type of change that occurred in the vector.
            /// </summary>
            public CollectionChange CollectionChange { get; private set; }

            /// <summary>
            /// Gets the position where the change occurred in the vector.
            /// </summary>
            public uint Index { get; private set; }
        }

        internal void RebindCurrent()
        {
            try
            {
                RangeMovable = false;
                if (CurrentChanged != null)
                    CurrentChanged(this, CurrentItem);
            }
            finally
            {
                RangeMovable = true;
            }
        }
    }

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

    public class RangedCollectionDataSource : DependencyObject
    {
        public static readonly DependencyProperty DataSourceProperty = DependencyProperty.RegisterAttached(
            "DataSource",
            typeof(object),
            typeof(RangedCollectionDataSource), new PropertyMetadata(null, DataSourceChanged)
            );

        private static void DataSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = d as Selector;
            var rcb = e.NewValue as RangedCollectionBase;
            try
            {
                if (rcb != null)
                    rcb.Movable = false;

                target.ItemsSource = rcb;

                var targetItem = rcb.CurrentItem;
                if(target is ListViewBase && targetItem != null)
                    d.Dispatcher.RunIdleAsync((arg) => ((ListViewBase)target).ScrollIntoView(targetItem, ScrollIntoViewAlignment.Leading));
            }
            finally
            {
                if (rcb != null)
                    rcb.Movable = true;
            }
        }

        public static void SetDataSource(UIElement element, object value)
        {
            element.SetValue(DataSourceProperty, value);
        }

        public static object GetDataSource(UIElement element)
        {
            return element.GetValue(DataSourceProperty);
        }
    }
}
