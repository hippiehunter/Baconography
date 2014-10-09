using GalaSoft.MvvmLight;
using Newtonsoft.Json;
using SnooSharp;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
						Debug.Assert(context is SnooStreamViewModel);
						var snooStreamViewModel = (SnooStreamViewModel)context;
						var subredditThing = JsonConvert.DeserializeObject<Subreddit>(stateItem.Item2);
						return snooStreamViewModel.SubredditRiver.CombinedRivers.FirstOrDefault(vm => vm.Thing.Id == subredditThing.Id);
                    }
				//case "LinkStreamViewModel":
				//	{
				//		return new LinkStreamViewModel(context, stateItem.Item2);
				//	}
				case "CommentsViewModel":
					{
						var dumpArgs = JsonConvert.DeserializeObject<Tuple<Listing, string, string>>(stateItem.Item2);
						LinkViewModel targetContext = null;
						//if (context is LinkStreamViewModel)
						//	targetContext = ((LinkStreamViewModel)context).Current; else
						if (context is LinkRiverViewModel)
							targetContext = ((LinkRiverViewModel)context).Links.FirstOrDefault(link => link.Link.Id == dumpArgs.Item3);

						var comments = new CommentsViewModel(targetContext, dumpArgs.Item1, dumpArgs.Item2);
						if (targetContext != null)
							targetContext.Comments = comments;

						return comments;
					}
                case "SettingsViewModel":
                    {
                        return new SettingsViewModel(SnooStreamViewModel.Settings);
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
                var serialized = JsonConvert.SerializeObject(linkRiver.Thing);
                return JsonConvert.SerializeObject(Tuple.Create("LinkRiverViewModel", serialized));
            }
			//else if (viewModel is LinkStreamViewModel)
			//{
			//	var linkStream = viewModel as LinkStreamViewModel;
			//	return JsonConvert.SerializeObject(Tuple.Create("LinkStreamViewModel", ((LinkViewModel)linkStream.Visible.Context).Link.Id));
			//}
			else if (viewModel is CommentsViewModel)
			{
				var comments = viewModel as CommentsViewModel;
				return JsonConvert.SerializeObject(Tuple.Create("CommentsViewModel", JsonConvert.SerializeObject(Tuple.Create(comments.DumpListing(), comments.Link.Url, comments.Link.Link.Id))));
			}
            else if (viewModel is LockScreenViewModel
                || viewModel is SettingsViewModel)
            {
                return JsonConvert.SerializeObject(Tuple.Create("SettingsViewModel", ""));
            }
			else
				throw new InvalidOperationException();
        }
    }
}
