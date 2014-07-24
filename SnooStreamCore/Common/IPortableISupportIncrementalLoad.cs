using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Common
{
	public interface PortableISupportIncrementalLoad : ICollection, INotifyCollectionChanged, INotifyPropertyChanged, IList
	{
		bool HasMoreItems { get; }
		Task<int> LoadMoreItemsAsync(uint count);
	}
}
