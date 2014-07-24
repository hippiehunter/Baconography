using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Common
{
	public class PortableObservableCollection<T> : ObservableCollection<T>, PortableISupportIncrementalLoad
	{
		Func<Task> _loadMore;
		public PortableObservableCollection(Func<Task> loadMore, IEnumerable<T> initialCollection) : base(initialCollection)
		{
			_loadMore = loadMore;
			HasMoreItems = true;
		}

		public PortableObservableCollection(Func<Task> loadMore)
		{
			_loadMore = loadMore;
			HasMoreItems = true;
		}

		public bool HasMoreItems { get; set; }

		public async Task<int> LoadMoreItemsAsync(uint count)
		{
			var oldSize = Count;
			await _loadMore();
			HasMoreItems = Count != oldSize;
			return Count - oldSize;
		}
	}
}
