using GalaSoft.MvvmLight;
using Newtonsoft.Json;
using SnooSharp;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public class ViewModelDumpUtility
    {
        public static ViewModelBase LoadFromDump(string dump, ViewModelBase context)
        {
            var stateItem = JsonConvert.DeserializeObject<Tuple<string, string>>(dump);
            switch (stateItem.Item1)
            {
                case "LinkRiverViewModel":
                    {
                        var dumpArgs = JsonConvert.DeserializeObject<Tuple<bool, SnooSharp.Subreddit, string, List<SnooSharp.Link>>>(stateItem.Item2);
                        return new LinkRiverViewModel(dumpArgs.Item1, dumpArgs.Item2, dumpArgs.Item3, dumpArgs.Item4);
                    }
				case "LinkStreamViewModel":
					{
						return new LinkStreamViewModel(context, stateItem.Item2);
					}
				case "CommentsViewModel":
					{
						var dumpArgs = JsonConvert.DeserializeObject<Tuple<Listing, string>>(stateItem.Item2);
						return new CommentsViewModel(context is LinkStreamViewModel ? ((LinkStreamViewModel)context).Current : context, dumpArgs.Item1, dumpArgs.Item2);
					}
                default:
                    throw new InvalidOperationException();
            }
        }

        public static string Dump(ViewModelBase viewModel)
        {
            if (viewModel is LinkRiverViewModel)
            {
                var linkRiver = viewModel as LinkRiverViewModel;
                var serialized = JsonConvert.SerializeObject(Tuple.Create(linkRiver.IsLocal, linkRiver.Thing, linkRiver.Sort, linkRiver.Select(vm => vm.Link).ToList()));
                return JsonConvert.SerializeObject(Tuple.Create("LinkRiverViewModel", serialized));
            }
			else if (viewModel is LinkStreamViewModel)
			{
				var linkStream = viewModel as LinkStreamViewModel;
				return JsonConvert.SerializeObject(Tuple.Create("LinkStreamViewModel", ((LinkViewModel)linkStream.Visible.Context).Link.Id));
			}
			else if (viewModel is CommentsViewModel)
			{
				var comments = viewModel as CommentsViewModel;
				return JsonConvert.SerializeObject(Tuple.Create("CommentsViewModel", JsonConvert.SerializeObject(Tuple.Create(comments.DumpListing(), comments.Link.Url))));
			}
			else
				throw new InvalidOperationException();
        }
    }
}
