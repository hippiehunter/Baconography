using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public interface IMergableViewModel<T>
    {
        string MergeID { get; }
        void Merge(T source);
    }

    public class CollectionMerger
    {
        public static void Merge<T>(ObservableCollection<object> destination, IEnumerable<IMergableViewModel<T>> source)
        {
            //build 
        }
    }
}
