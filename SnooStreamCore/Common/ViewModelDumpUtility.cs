﻿using GalaSoft.MvvmLight;
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
						var subredditThing = JsonConvert.DeserializeObject<Tuple<Subreddit, string, List<Link>, DateTime, string>>(stateItem.Item2);
						var result = snooStreamViewModel.SubredditRiver.CombinedRivers.FirstOrDefault(vm => vm.Thing.Id == subredditThing.Item1.Id);
						if (result == null)
						{
							result = new LinkRiverViewModel(false, subredditThing.Item1, subredditThing.Item2, subredditThing.Item3, subredditThing.Item4);
						}

						result.CurrentSelected = result.Links.FirstOrDefault(lnk => lnk.Id == subredditThing.Item5);
						return result;
                    }
				//case "LinkStreamViewModel":
				//	{
				//		return new LinkStreamViewModel(context, stateItem.Item2);
				//	}
				case "CommentsViewModel":
					{
						var dumpArgs = JsonConvert.DeserializeObject<Tuple<Listing, string, string, DateTime?>>(stateItem.Item2);
						LinkViewModel targetContext = null;
						//if (context is LinkStreamViewModel)
						//	targetContext = ((LinkStreamViewModel)context).Current; else
						if (context is LinkRiverViewModel)
							targetContext = ((LinkRiverViewModel)context).Links.FirstOrDefault(link => link.Id == dumpArgs.Item3) as LinkViewModel;

						var comments = new CommentsViewModel(targetContext, dumpArgs.Item1, dumpArgs.Item2, dumpArgs.Item4);
						if (targetContext != null)
							targetContext.Comments = comments;

						return comments;
					}
                case "SettingsViewModel":
                    {
                        return new SettingsViewModel(SnooStreamViewModel.Settings);
                    }
				case "PostViewModel":
					{
						var dumpArgs = JsonConvert.DeserializeAnonymousType(stateItem.Item2, new { Editing = false, Kind = "", Subreddit = "", Text = "", Title = "", Url = "" });
						return new PostViewModel()
						{
							Editing = dumpArgs.Editing,
							Kind = dumpArgs.Kind,
							Subreddit = dumpArgs.Subreddit,
							Text = dumpArgs.Text,
							Title = dumpArgs.Title,
							Url = dumpArgs.Url,
						};
					}
				case "MessageViewModel":
					{
						var postViewModel = new CreateMessageViewModel();
						return postViewModel;
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
				string selectedId = null;
				if(linkRiver.CurrentSelected != null)
					selectedId = linkRiver.CurrentSelected.Id;

				var serializationTpl = new Tuple<Subreddit, string, List<Link>, DateTime, string>(linkRiver.Thing, linkRiver.Sort, 
					linkRiver.Links
						.Take(100)
						.Select(lvm => ((LinkViewModel)lvm).Link)
						.ToList(), 
					linkRiver.LastRefresh ?? DateTime.Now, selectedId);
				var serialized = JsonConvert.SerializeObject(serializationTpl);
                return JsonConvert.SerializeObject(Tuple.Create("LinkRiverViewModel", serialized));
            }
			//else if (viewModel is ContentStreamViewModel)
			//{
			//	var contentStream = viewModel as ContentStreamViewModel;
			//	return JsonConvert.SerializeObject(Tuple.Create("ContentStreamViewModel", ((LinkViewModel)contentStream.).Link.Id));
			//}
			else if (viewModel is CommentsViewModel)
			{
				var comments = viewModel as CommentsViewModel;
				return JsonConvert.SerializeObject(Tuple.Create("CommentsViewModel", JsonConvert.SerializeObject(Tuple.Create(comments.DumpListing(), comments.Link.Url, comments.Link.Link.Id, comments.LastRefresh))));
			}
            else if (viewModel is LockScreenViewModel
                || viewModel is SettingsViewModel)
            {
                return JsonConvert.SerializeObject(Tuple.Create("SettingsViewModel", ""));
            }
			else if (viewModel is PostViewModel)
			{
				var postViewModel = viewModel as PostViewModel;
				return JsonConvert.SerializeObject(new { Editing = postViewModel.Editing, Kind = postViewModel.Kind, Subreddit = postViewModel.Subreddit, Text = postViewModel.Text, Title = postViewModel.Title, Url = postViewModel.Url });
			}
			else
				throw new InvalidOperationException();
        }
    }
}
