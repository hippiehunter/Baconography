using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SnooStream.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Test
{
    [TestClass]
    public class CollectionMerge
    {
        [TestMethod]
        public void MergeTest()
        {
            Random rnd = new Random();
            for (int i = 0; i < 100000; i++)
            {
                HashSet<int> mergeTargetIds = new HashSet<int>();
                while (mergeTargetIds.Count < 50)
                {
                    var rndInt = rnd.Next(0, 200);
                    if (!mergeTargetIds.Contains(rndInt))
                    {
                        mergeTargetIds.Add(rndInt);
                    }
                }

                var mergeTarget = RandomArrayTool.Randomize(mergeTargetIds.Select(id => (object)new MergableInt { MergeID = id.ToString() }).ToList());

                HashSet<int> mergeSourceIds = new HashSet<int>();
                while (mergeSourceIds.Count < 50)
                {
                    var rndInt = rnd.Next(0, 200);
                    if (!mergeSourceIds.Contains(rndInt))
                    {
                        mergeSourceIds.Add(rndInt);
                    }
                }

                var mergeSource = RandomArrayTool.Randomize(mergeSourceIds.Select(id => new MergableInt { MergeID = id.ToString() }).ToList());
                CollectionMerger.Merge(mergeTarget, mergeSource);

                Assert.AreEqual(mergeSource.Count, mergeTarget.Count);
                for (int ii = 0; ii < mergeSource.Count; ii++)
                {
                    Assert.AreEqual(mergeSource[ii].MergeID, ((MergableInt)mergeTarget[ii]).MergeID);
                }
            }
            

        }

        private class MergableInt : IMergableViewModel<MergableInt>
        {
            public string MergeID { get; set; }
            public void Merge(MergableInt source)
            {
                Debug.Assert(source.MergeID == MergeID, "bad merge");

            }
        }

        static class RandomArrayTool
        {
            static Random _random = new Random();

            public static ObservableCollection<T> Randomize<T>(IList<T> arr)
            {
                List<KeyValuePair<int, T>> list = new List<KeyValuePair<int, T>>();
                // Add all strings from array
                // Add new random int each time
                foreach (T s in arr)
                {
                    list.Add(new KeyValuePair<int, T>(_random.Next(), s));
                }
                // Sort the list by the random number
                var sorted = from item in list
                             orderby item.Key
                             select item;
                // Allocate new string array
                ObservableCollection<T> result = new ObservableCollection<T>();
                foreach (KeyValuePair<int, T> pair in sorted)
                {
                    result.Add(pair.Value);
                }
                // Return copied array
                return result;
            }
        }
    }
}
