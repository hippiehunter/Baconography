using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public class ObservableSortedUniqueCollection<TKey, TValue> : IDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged, ICollection<TValue> where TValue : class
    {
        private SortedList<TValue, TValue> _realSorted;
        private Dictionary<TKey, TValue> _realLookup;

        public ObservableSortedUniqueCollection(Dictionary<TKey, TValue> initialData, IComparer<TValue> comparer)
        {
            _realLookup = initialData;
            _realSorted = new SortedList<TValue, TValue>(comparer);
            foreach (var initial in initialData)
            {
                _realSorted.Add(initial.Value, initial.Value);
            }

        }

        public ObservableSortedUniqueCollection(IComparer<TValue> comparer)
        {
            _realSorted = new SortedList<TValue, TValue>(comparer);
            _realLookup = new Dictionary<TKey, TValue>();
        }


        public void Add(TKey key, TValue value)
        {
            _realLookup.Add(key, value);
            var indexOfAdd = _realSorted.Add(value, value);
            if (CollectionChanged != null)
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value, indexOfAdd));
        }

        public bool ContainsKey(TKey key)
        {
            return _realLookup.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get { return _realLookup.Keys; }
        }

        public bool Remove(TKey key)
        {
            TValue removedElement;
            if (!_realLookup.TryGetValue(key, out removedElement))
                return false;

            _realLookup.Remove(key);
            var indexOfRemoveal = _realSorted.IndexOfKey(removedElement);
            if(indexOfRemoveal != -1)
                _realSorted.RemoveAt(indexOfRemoveal);

            if (CollectionChanged != null && indexOfRemoveal != -1)
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedElement, indexOfRemoveal));

            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _realLookup.TryGetValue(key, out value);
        }

        public ICollection<TValue> Values
        {
            get { return _realSorted.Values; }
        }

        public TValue this[TKey key]
        {
            get
            {
                return _realLookup[key];
            }
            set
            {
                var originalValue = _realLookup[key];
                if (originalValue != value)
                {
                    var indexOfOriginal = _realSorted.IndexOfKey(originalValue);
                    _realLookup[key] = value;
                    _realSorted.Remove(originalValue);
                    _realSorted.Add(value, value);
                    var indexOfReplacement = _realSorted.IndexOfKey(value);

                    if (indexOfReplacement == indexOfOriginal)
                    {
                        if (CollectionChanged != null)
                            CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, indexOfReplacement));
                    }
                    else
                    {
                        //TODO this might be a problem for the controls dont know what the pattern is expected to be here
                        if (CollectionChanged != null)
                        {
                            CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, value, indexOfReplacement, indexOfOriginal));
                            CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, indexOfReplacement));
                        }
                    }
                }
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _realLookup.Clear();
            _realSorted.Clear();
            if (CollectionChanged != null)
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return _realSorted.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return _realLookup.GetEnumerator();
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            return _realSorted.Values.GetEnumerator();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public void Add(TValue item)
        {
            throw new NotImplementedException();
        }

        public bool Contains(TValue item)
        {
            return _realSorted.ContainsKey(item);
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(TValue item)
        {
            throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
