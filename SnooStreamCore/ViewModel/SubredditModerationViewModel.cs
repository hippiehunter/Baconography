using GalaSoft.MvvmLight;
using SnooSharp;
using SnooStream.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class SubredditModerationViewModel : ViewModelBase
    {
        public class QueuedItem : ViewModelBase
        {
            public string Title {get; set;}
            public string Body {get; set;}
            public bool IsComment {get; set;}
            public ActivityViewModel Activity {get; set;}
        }

        public class ModQueueLoader : IIncrementalCollectionLoader<QueuedItem>
        {
            SubredditModerationViewModel _modVM;
            public ModQueueLoader(SubredditModerationViewModel modVM)
            {
                _modVM = modVM;
            }

            public Task AuxiliaryItemLoader(IEnumerable<QueuedItem> items, int timeout)
            {
                return Task.FromResult(true);
            }

            public bool IsStale
            {
                get { return _modVM.LastRefresh == null || (DateTime.Now - _modVM.LastRefresh.Value).TotalMinutes > 30; }
            }

            public bool HasMore()
            {
                return SnooStreamViewModel.RedditUserState.IsMod && _modVM.LastQueueId != null;
            }

            IEnumerable<QueuedItem> ProcessListing(Listing listing)
            {
                var result = new List<QueuedItem>();
                foreach (var child in listing.Data.Children)
                {
                    var activity = ActivityViewModel.CreateActivity(child);
                    if (child.Data is Comment)
                    {
                        result.Add(new QueuedItem
                        {
                            Activity = activity,
                            IsComment = true,
                            Body = activity.PreviewBody,
                            Title = activity.Title
                        });
                    }
                    else if (child.Data is Link)
                    {
                        result.Add(new QueuedItem
                        {
                            Activity = activity,
                            IsComment = false,
                            Body = activity.PreviewBody,
                            Title = activity.Title
                        });
                    }
                }
                return result;
            }

            public async Task<IEnumerable<QueuedItem>> LoadMore()
            {
                var additional = await SnooStreamViewModel.RedditService.GetAdditionalFromListing(string.Format(Reddit.SubredditAboutBaseUrlFormat, Reddit.MakePlainSubredditName(_modVM.Thing.Url), "moderator"), _modVM.LastQueueId);
                _modVM.LastQueueId = additional.Data.After;
                return ProcessListing(additional);
            }

            public async Task Refresh(System.Collections.ObjectModel.ObservableCollection<QueuedItem> current, bool onlyNew)
            {
                var modQueue = await SnooStreamViewModel.RedditService.GetModQueue(Reddit.MakePlainSubredditName(_modVM.Thing.Url), null);
                _modVM.LastQueueId = modQueue.Data.After;
                current.Clear();
                foreach (var item in ProcessListing(modQueue))
                {
                    current.Add(item);
                }
            }

            public string NameForStatus
            {
                get { return "mod activitie"; }
            }

            public void Attach(System.Collections.ObjectModel.ObservableCollection<QueuedItem> targetCollection)
            {
                
            }
        }

        public class ModActivityLoader : IIncrementalCollectionLoader<ModeratorActivityViewModel>
        {
            SubredditModerationViewModel _modVM;
            public ModActivityLoader(SubredditModerationViewModel modVM)
            {
                _modVM = modVM;
            }

            public Task AuxiliaryItemLoader(IEnumerable<ModeratorActivityViewModel> items, int timeout)
            {
                return Task.FromResult(true);
            }

            public bool IsStale
            {
                get { return _modVM.LastRefresh == null || (DateTime.Now - _modVM.LastRefresh.Value).TotalMinutes > 30; }
            }

            public bool HasMore()
            {
                return SnooStreamViewModel.RedditUserState.IsMod && !string.IsNullOrWhiteSpace(_modVM.LastLogId);
            }

            public async Task<IEnumerable<ModeratorActivityViewModel>> LoadMore()
            {
                var additional = await SnooStreamViewModel.RedditService.GetAdditionalFromListing(string.Format(Reddit.SubredditAboutBaseUrlFormat, Reddit.MakePlainSubredditName(_modVM.Thing.Url), "log"), _modVM.LastLogId);
                _modVM.LastLogId = additional.Data.After;
                return ProcessListing(additional);
            }

            public async Task Refresh(System.Collections.ObjectModel.ObservableCollection<ModeratorActivityViewModel> current, bool onlyNew)
            {
                var modQueue = await SnooStreamViewModel.RedditService.GetModActions(Reddit.MakePlainSubredditName(_modVM.Thing.Url), null);
                _modVM.LastLogId = modQueue.Data.After;
                current.Clear();
                foreach (var item in ProcessListing(modQueue))
                {
                    current.Add(item);
                }
            }

            IEnumerable<ModeratorActivityViewModel> ProcessListing(Listing listing)
            {
                var result = new List<ModeratorActivityViewModel>();
                foreach (var child in listing.Data.Children)
                {
                    var activity = ActivityViewModel.CreateActivity(child);
                    if (child.Data is ModAction && activity is ModeratorActivityViewModel)
                    {
                        result.Add(activity as ModeratorActivityViewModel);
                    }
                }
                return result;
            }

            public string NameForStatus
            {
                get { return "mod activitie"; }
            }

            public void Attach(System.Collections.ObjectModel.ObservableCollection<ModeratorActivityViewModel> targetCollection)
            {
               
            }
        }

        public string LastQueueId { get; set; }
        public string LastLogId { get; set; }
        public DateTime? LastRefresh { get; set; }
        public Subreddit Thing {get; set;}
        public SubredditModerationViewModel(Subreddit thing)
        {
            Thing = thing;
            ModQueue = SnooStreamViewModel.SystemServices.MakeIncrementalLoadCollection<QueuedItem>(new ModQueueLoader(this));
            ModCommentQueue = SnooStreamViewModel.SystemServices.MakeFilteredIncrementalLoadCollection(ModQueue, (item) => item.IsComment);
            ModLinkQueue = SnooStreamViewModel.SystemServices.MakeFilteredIncrementalLoadCollection(ModQueue, (item) => !item.IsComment);
            ModLog = SnooStreamViewModel.SystemServices.MakeIncrementalLoadCollection<ModeratorActivityViewModel>(new ModActivityLoader(this));
        }

        public ObservableCollection<QueuedItem> ModQueue { get; set; }
        public ObservableCollection<QueuedItem> ModLinkQueue { get; set; }
        public ObservableCollection<QueuedItem> ModCommentQueue { get; set; }
        public ObservableCollection<ModeratorActivityViewModel> ModLog { get; set; }
    }
}
