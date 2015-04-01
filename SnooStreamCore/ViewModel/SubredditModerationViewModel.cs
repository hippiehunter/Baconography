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
        public class QueuedItem
        {
            public string Title {get; set;}
            public string Body {get; set;}
            public bool IsComment {get; set;}
            public ActivityViewModel Activity {get; set;}
        }

        public class ModQueueLoader : IIncrementalCollectionLoader<ViewModelBase>
        {
            SubredditModerationViewModel _modVM;
            public ModQueueLoader(SubredditModerationViewModel modVM)
            {
                _modVM = modVM;
            }

            public Task AuxiliaryItemLoader(IEnumerable<ViewModelBase> items, int timeout)
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

            IEnumerable<ViewModelBase> ProcessListing(Listing listing)
            {
                List<ViewModelBase> result = new List<ViewModelBase>();
                foreach (var child in listing.Data.Children)
                {
                    if (child.Data is Comment)
                    {

                    }
                    else if (child.Data is Link)
                    {

                    }
                }
                return result;
            }

            public async Task<IEnumerable<ViewModelBase>> LoadMore()
            {
                var additional = await SnooStreamViewModel.RedditService.GetAdditionalFromListing(string.Format(Reddit.SubredditAboutBaseUrlFormat, Reddit.MakePlainSubredditName(_modVM.Thing.Url), "moderator"), _modVM.LastQueueId);
                _modVM.LastQueueId = additional.Data.After;
                return ProcessListing(additional);
            }

            public async Task Refresh(System.Collections.ObjectModel.ObservableCollection<ViewModelBase> current, bool onlyNew)
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

            public void Attach(System.Collections.ObjectModel.ObservableCollection<ViewModelBase> targetCollection)
            {
                
            }
        }

        public class ModActivityLoader : IIncrementalCollectionLoader<ViewModelBase>
        {
            ModStreamViewModel _modStream;
            ActivityGroupViewModel.ActivityAggregate _activityAggregate;
            public ModActivityLoader(ModStreamViewModel modStream)
            {
                _modStream = modStream;
            }

            public Task AuxiliaryItemLoader(IEnumerable<ViewModelBase> items, int timeout)
            {
                return Task.FromResult(true);
            }

            public bool IsStale
            {
                get { return _modStream.LastRefresh == null || (DateTime.Now - _modStream.LastRefresh.Value).TotalMinutes > 30; }
            }

            public bool HasMore()
            {
                var anyQueue = _modStream.Subreddits.Any(sr => sr.Enabled && !string.IsNullOrWhiteSpace(sr.OldestQueue));
                //TODO deal with active subreddit mods
                return SnooStreamViewModel.RedditUserState.IsMod && (_modStream.OldestModMail != null || anyQueue);
            }

            public async Task<IEnumerable<ViewModelBase>> LoadMore()
            {
                await _modStream.PullOlder();
                return Enumerable.Empty<ViewModelBase>();
            }

            public async Task Refresh(System.Collections.ObjectModel.ObservableCollection<ViewModelBase> current, bool onlyNew)
            {
                await _modStream.PullNew(false, !onlyNew);
            }

            public string NameForStatus
            {
                get { return "mod activitie"; }
            }

            public void Attach(System.Collections.ObjectModel.ObservableCollection<ViewModelBase> targetCollection)
            {
                _activityAggregate = new ActivityGroupViewModel.ActivityAggregate(_modStream.Groups, targetCollection);
            }
        }

        public string LastQueueId { get; set; }
        public string LastLogId { get; set; }
        public DateTime? LastRefresh { get; set; }
        public Subreddit Thing {get; set;}
        public SubredditModerationViewModel(Subreddit thing)
        {
            Thing = thing;
        }

        public ObservableCollection<QueuedItem> ModQueue { get; set; }
        public ObservableCollection<ModeratorActivityViewModel> ModLog { get; set; }
    }
}
