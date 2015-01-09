using GalaSoft.MvvmLight;
using MetroLog;
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
        static ILogger _logger = LogManagerFactory.DefaultLogManager.GetLogger<ViewModelDumpUtility>();
        public static ViewModelBase LoadFromDump(string dump, ViewModelBase context, SnooStreamViewModel rootContext)
        {
            try
            {
                var stateItem = JsonConvert.DeserializeObject<Tuple<string, string>>(dump);
                switch (stateItem.Item1)
                {
                    //case "CommentsContentStreamViewModel":
                    //	{
                    //		break;
                    //	}
                    case "LinkRiverViewModel":
                        {
                            Debug.Assert(context is SnooStreamViewModel);
                            var subredditThing = JsonConvert.DeserializeObject<Tuple<Subreddit, string, List<Link>, DateTime, string>>(stateItem.Item2);
                            var result = rootContext.SubredditRiver.CombinedRivers.FirstOrDefault(vm => vm.Thing.Id == subredditThing.Item1.Id);
                            if (result == null)
                            {
                                result = new LinkRiverViewModel(false, subredditThing.Item1, subredditThing.Item2, subredditThing.Item3, subredditThing.Item4);
                            }

                            result.CurrentSelected = result.Links.FirstOrDefault(lnk => lnk.Id == subredditThing.Item5);
                            return result;
                        }
                    case "CommentsViewModel":
                        {
                            var dumpArgs = JsonConvert.DeserializeObject<Tuple<Listing, string, string, DateTime?>>(stateItem.Item2);
                            LinkViewModel targetContext = null;
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
                    case "ConversationViewModel":
                        {
                            ActivityGroupViewModel targetGroup;
                            if (rootContext.SelfStream.Groups.TryGetValue(stateItem.Item2, out targetGroup))
                            {
                                return new ConversationViewModel(targetGroup, rootContext.SelfStream);
                            }
                            else
                                throw new InvalidOperationException(stateItem.Item1);
                        }
                    case "CommentsContentStreamViewModel":
                        {
                            var dumpArgs = JsonConvert.DeserializeObject<Tuple<string, string>>(stateItem.Item2);
                            Debug.Assert(context is CommentsViewModel);
                            var commentsViewModel = context as CommentsViewModel;
                            var streamViewModel = new CommentsContentStreamViewModel(commentsViewModel);
                            streamViewModel.CurrentSelected = streamViewModel.Links.FirstOrDefault(linkVM => linkVM.Id == dumpArgs.Item1 && linkVM.Url == dumpArgs.Item2);
                            return streamViewModel;
                        }
                    default:
                        throw new InvalidOperationException(stateItem.Item1);
                }
            }
            catch (Exception ex)
            {
                _logger.Fatal(string.Format("dump was {0}", dump), ex);
                throw;
            }
        }

        public static string Dump(ViewModelBase viewModel)
        {
            try
            {
                if (viewModel is LinkRiverViewModel)
                {
                    var linkRiver = viewModel as LinkRiverViewModel;
                    string selectedId = null;
                    if (linkRiver.CurrentSelected != null)
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
                    return JsonConvert.SerializeObject(Tuple.Create("PostViewModel", JsonConvert.SerializeObject(new { Editing = postViewModel.Editing, Kind = postViewModel.Kind, Subreddit = postViewModel.Subreddit, Text = postViewModel.Text, Title = postViewModel.Title, Url = postViewModel.Url })));
                }
                else if (viewModel is ConversationViewModel)
                {
                    var conversationViewModel = viewModel as ConversationViewModel;
                    return JsonConvert.SerializeObject(Tuple.Create("ConversationViewModel", conversationViewModel.CurrentGroup.Id));
                }
                else if (viewModel is CommentsContentStreamViewModel)
                {
                    var contentStream = viewModel as CommentsContentStreamViewModel;
                    var currentSelectedUrl = contentStream.CurrentSelected.Url;
                    var currentSelectedId = contentStream.CurrentSelected.Id;
                    return JsonConvert.SerializeObject(Tuple.Create("CommentsContentStreamViewModel", JsonConvert.SerializeObject(Tuple.Create(currentSelectedId, currentSelectedUrl))));
                }
                else
                    throw new InvalidOperationException(viewModel.GetType().FullName);
            }
            catch (Exception ex)
            {
                _logger.Fatal(string.Format("type was {0}", viewModel != null ? viewModel.GetType().FullName : "null"), ex);
                throw;
            }
        }
    }
}
