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
    public class AboutRedditViewModel : ViewModelBase, IRefreshable
    {
        public class RecommendedSubredditLoader : IIncrementalCollectionLoader<ViewModelBase>
        {
            public DateTime? LastRefresh;
            AboutRedditViewModel _subreddit;
            ObservableCollection<ViewModelBase> _targetCollection;
            bool _hasLoaded;
            public RecommendedSubredditLoader(AboutRedditViewModel subreddit)
            {
                _hasLoaded = false;
                _subreddit = subreddit;
            }

            public Task AuxiliaryItemLoader(IEnumerable<ViewModelBase> items, int timeout)
            {
                return Task.FromResult(true);
            }

            public bool IsStale
            {
                get { return LastRefresh == null || (DateTime.Now - LastRefresh.Value).TotalMinutes > 30; }
            }

            public bool HasMore()
            {
                return !_hasLoaded;
            }

            public async Task<IEnumerable<ViewModelBase>> LoadMore()
            {
                _hasLoaded = true;
                return await PullNew();
            }

            public async Task Refresh(System.Collections.ObjectModel.ObservableCollection<ViewModelBase> current, bool onlyNew)
            {
                _hasLoaded = true;
                await PullNew();
            }

            public string NameForStatus
            {
                get { return _subreddit.Thing.Headertitle + " recommendation"; }
            }

            public void Attach(System.Collections.ObjectModel.ObservableCollection<ViewModelBase> targetCollection)
            {
                _targetCollection = targetCollection;
            }

            async Task<IEnumerable<ViewModelBase>> PullNew()
            {
                List<ViewModelBase> result = new List<ViewModelBase>();
                var listing = await SnooStreamViewModel.RedditService.GetRecomendedSubreddits(new List<string> { _subreddit.Thing.Url });
                if (listing == null || listing.Count == 0)
                {
                    var search = await SnooStreamViewModel.RedditService.Search(_subreddit.Thing.DisplayName, null, true, null);
                    if (search != null && search.Item2 != null && search.Item2.Data != null && search.Item2.Data.Children != null)
                    {
                        foreach (var thing in search.Item2.Data.Children)
                        {
                            if(thing.Data is Subreddit)
                                result.Add(new AboutRedditViewModel(thing.Data as Subreddit, DateTime.Now));
                        }
                    }
                }
                else
                {
                    foreach (var item in listing)
                    {
                        result.Add(new AboutRedditViewModel(item.Subreddit));
                    }
                }
                LastRefresh = DateTime.UtcNow;
                return result;
            }
        }

        public class SubredditModeratorLoader : IIncrementalCollectionLoader<ViewModelBase>
        {
            public DateTime? LastRefresh;
            AboutRedditViewModel _subreddit;
            ObservableCollection<ViewModelBase> _targetCollection;
            bool _hasLoaded;
            public SubredditModeratorLoader(AboutRedditViewModel subreddit)
            {
                _hasLoaded = false;
                _subreddit = subreddit;
            }

            public Task AuxiliaryItemLoader(IEnumerable<ViewModelBase> items, int timeout)
            {
                return Task.FromResult(true);
            }

            public bool IsStale
            {
                get { return LastRefresh == null || (DateTime.Now - LastRefresh.Value).TotalMinutes > 30; }
            }

            public bool HasMore()
            {
                return !_hasLoaded;
            }

            public async Task<IEnumerable<ViewModelBase>> LoadMore()
            {
                _hasLoaded = true;
                return await PullNew();
            }

            public async Task Refresh(System.Collections.ObjectModel.ObservableCollection<ViewModelBase> current, bool onlyNew)
            {
                _hasLoaded = true;
                await PullNew();
            }

            public string NameForStatus
            {
                get { return _subreddit.Thing.Headertitle + " moderator"; }
            }

            public void Attach(System.Collections.ObjectModel.ObservableCollection<ViewModelBase> targetCollection)
            {
                _targetCollection = targetCollection;
            }

            async Task<IEnumerable<ViewModelBase>> PullNew()
            {
                List<ViewModelBase> result = new List<ViewModelBase>();
                var listing = await SnooStreamViewModel.RedditService.GetSubredditAbout(_subreddit.Thing.Url, "moderators");
                foreach (var item in listing.Data.Children)
                {
                    result.Add(new AboutUserViewModel(item.Name));
                }
                LastRefresh = DateTime.UtcNow;
                return result;
            }
        }

        public bool Loading { get; set; }
        public DateTime? LastRefresh { get; set; }
        public Subreddit Thing { get; set; }
        public int HeaderImageWidth { get { return GetHeaderSizeOrDefault(true); } }
        public int HeaderImageHeight { get { return GetHeaderSizeOrDefault(false); } }

        private const int DefaultHeaderWidth = 125;
        private const int DefaultHeaderHeight = 50;

        private int GetHeaderSizeOrDefault(bool width)
        {
            if (Thing.HeaderSize == null || Thing.HeaderSize.Length < 2)
                return width ? DefaultHeaderWidth : DefaultHeaderHeight;
            else
                return width ? Thing.HeaderSize[0] : Thing.HeaderSize[1];
        }

        public object DescriptionMD
        {
            get
            {
                return SnooStreamViewModel.MarkdownProcessor.Process(Thing.Description).MarkdownDom;
            }
        }
        public ObservableCollection<ViewModelBase> Recomendations { get; set; }
        public ObservableCollection<ViewModelBase> Moderators { get; set; }

        public AboutRedditViewModel(string subredditName)
        {
            Thing = new Subreddit(subredditName);
            LastRefresh = new DateTime();
            Recomendations = SnooStreamViewModel.SystemServices.MakeIncrementalLoadCollection(new RecommendedSubredditLoader(this));
            Moderators = SnooStreamViewModel.SystemServices.MakeIncrementalLoadCollection(new SubredditModeratorLoader(this));
        }

        public AboutRedditViewModel(Subreddit thing, DateTime? lastRefresh)
        {
            LastRefresh = lastRefresh ?? new DateTime();
            Thing = thing;
            Recomendations = SnooStreamViewModel.SystemServices.MakeIncrementalLoadCollection(new RecommendedSubredditLoader(this));
            Moderators = SnooStreamViewModel.SystemServices.MakeIncrementalLoadCollection(new SubredditModeratorLoader(this));
        }

        public Task MaybeRefresh()
        {
            var validLastRefresh = LastRefresh ?? new DateTime();
            if ((DateTime.UtcNow - validLastRefresh).TotalMinutes > 15)
                return Refresh(false);
            else
                return Task.FromResult<bool>(true);
        }

        public async Task Refresh(bool onlyNew)
        {
            if (Thing != null && !Loading)
            {
                try
                {
                    Loading = true;
                    RaisePropertyChanged("Loading");
                    Thing = (await SnooStreamViewModel.RedditService.GetSubredditAbout(Thing.Url)).TypedData;
                    RaisePropertyChanged("Thing");
                    RaisePropertyChanged("DescriptionMD");
                    LastRefresh = DateTime.UtcNow;
                }
                finally
                {
                    Loading = false;
                    RaisePropertyChanged("Loading");
                }
            }
        }
    }
}
