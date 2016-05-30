using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public interface IMergableViewModel<T> where T : class, IMergableViewModel<T>
    {
        string MergeID { get; }
        void Merge(T source);
    }

    public class CollectionMerger
    {
        public static void Merge<T>(ObservableCollection<object> destination, IEnumerable<IMergableViewModel<T>> source) where T : class, IMergableViewModel<T>
        {
            Dictionary<string, Tuple<int, int, IMergableViewModel<T>>> namePosMap = new Dictionary<string, Tuple<int, int, IMergableViewModel<T>>>();
            for (int i = 0; i < destination.Count; i++)
            {
                if (destination[i] is IMergableViewModel<T>)
                {
                    var mergableElement = destination[i] as IMergableViewModel<T>;
                    if (namePosMap.ContainsKey(mergableElement.MergeID))
                        continue;
                    else
                    {
                        namePosMap.Add(mergableElement.MergeID, Tuple.Create(i, -1, mergableElement));
                    }
                }
                else
                {
                    //remove and try this index again
                    destination.RemoveAt(i--);
                }
            }

            for (int i = 0; i < source.Count(); i++)
            {
                var sourceElement = source.ElementAt(i);
                if (namePosMap.ContainsKey(sourceElement.MergeID))
                {
                    var existing = namePosMap[sourceElement.MergeID];
                    existing.Item3.Merge(sourceElement as T);
                    namePosMap[sourceElement.MergeID] = Tuple.Create(existing.Item1, i, existing.Item3);
                }
                else
                {
                    namePosMap.Add(sourceElement.MergeID, Tuple.Create(-1, i, sourceElement));
                }
            }

            foreach (var tpl in namePosMap.Values.Where(tpl =>tpl.Item1 == -1).OrderBy(tpl => tpl.Item2))
            {
                destination.Insert(tpl.Item2, tpl.Item3);
            }

            foreach (var tpl in namePosMap.Values.OrderBy(tpl => tpl.Item2))
            {
                var sourceIndex = destination.IndexOf(tpl.Item3);
                if (sourceIndex == tpl.Item2)
                    continue;
                else if (tpl.Item2 != -1)
                {
                    destination.Move(sourceIndex, tpl.Item2);
                }
            }

            //remove all the extras so we dont end up with something stale floating around at the end of the collection
            while(destination.Count > source.Count())
                destination.RemoveAt(source.Count());
            
        }
    }
}
