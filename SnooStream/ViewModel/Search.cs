using SnooSharp;
using SnooStream.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;

namespace SnooStream.ViewModel
{
    public interface ISearchContext
    {
        bool HasAdditional { get; }
        Task<Listing> Load(IProgress<float> progress, CancellationToken token, bool ignoreCache);
        Task<Listing> LoadAdditional(IProgress<float> progress, CancellationToken token);
        void GotoSubreddit(string url);
        string Query { get; set; }
        string RestrictedToSubreddit { get; set; }
        bool SubredditsOnly { get; set; }
        //this is so we can take advantage of LinkBuilder in the search results
        ILinkBuilderContext LinkBuilderContext { get; }
        event Action ResultsInvalidated;
    }

    public class SearchViewModel
    {
        private ISearchContext _context;

        public SearchViewModel(ISearchContext searchContext)
        {
            _context = searchContext;
            Result = new SearchCollection(_context);
        }

        public string Query { get { return _context.Query; } set { _context.Query = value; } }
        public string RestrictedToSubreddit { get { return _context.RestrictedToSubreddit; } set { _context.RestrictedToSubreddit = value; } }
        public bool? SubredditsOnly { get { return _context.SubredditsOnly; } set { _context.SubredditsOnly = value ?? false; } }
        public SearchCollection Result { get; set; }

        public void KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                //force the search
            }
        }

        public void ForceSearch()
        {
        }

        public void TextChanged()
        {

        }
    }

    public static class SearchViewModelBuilder
    {
        public static IEnumerable<object> MakeViewModels(IEnumerable<Thing> things, ISearchContext builderContext)
        {
            List<object> result = new List<object>();
            foreach (var thing in things)
            {
                if (thing.Data is Link)
                {
                    result.Add(new LinkViewModel { Context = builderContext.LinkBuilderContext.LinkContext, Thing = thing.Data as Link, Votable = new VotableViewModel(thing.Data as Link, builderContext.LinkBuilderContext.UpdateVotable), CommentCount = ((Link)thing.Data).CommentCount, FromMultiReddit = builderContext.LinkBuilderContext.IsMultiReddit });
                }
                else if (thing.Data is Subreddit)
                {
                    result.Add(new SearchSubredditViewModel { Context = builderContext, Thing = thing.Data as Subreddit, Name = ((Subreddit)thing.Data).DisplayName });
                }
            }
            return result;
        }
    }

    public class SearchSubredditViewModel
    {
        public ISearchContext Context { get; set; }
        public string Name { get; set; }
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

        public void Navigate()
        {
            Context.GotoSubreddit(Thing.Url);
        }
    }

    public class SearchCollection : RangedCollectionBase
    {
        public ISearchContext Context { get; set; }

        public SearchCollection(ISearchContext context)
        {
            Context = context;
            Context.ResultsInvalidated += Context_ResultsInvalidated;
        }

        private async void Context_ResultsInvalidated()
        {
            Clear();
            await this.LoadMoreItemsAsync(40);
        }

        public override bool HasMoreItems
        {
            get
            {
                return Context.HasAdditional;
            }
        }

        public override IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            //Load Additional
            if (Count > 0)
            {
                var loadItem = new LoadViewModel { LoadAction = async (progress, token) =>
                {
                    var loadedListing = await Context.LoadAdditional(progress, token);
                    AddRange(SearchViewModelBuilder.MakeViewModels(loadedListing.Data.Children, Context));
                }, IsCritical = false };
                Add(loadItem);
                return LoadItem(loadItem).AsAsyncOperation();
            }
            else //Load fresh
            {
                var loadItem = new LoadViewModel { LoadAction = async (progress, token) =>
                {
                    var loadedListing = await Context.Load(progress, token, false);
                    AddRange(SearchViewModelBuilder.MakeViewModels(loadedListing.Data.Children, Context));
                }, IsCritical = true };
                Add(loadItem);
                return LoadItem(loadItem).AsAsyncOperation();
            }
        }

        private void AddRange(IEnumerable<object> collection)
        {
            foreach (var item in collection)
                Add(item);
        }

        private async Task<LoadMoreItemsResult> LoadItem(LoadViewModel loadItem)
        {
            var itemCount = Count - 1;
            await loadItem.LoadAsync();
            //now that the load is finished the load item should be removed from the list
            Remove(loadItem);
            return new LoadMoreItemsResult { Count = (uint)(Count - itemCount) };
        }
    }

    public class SearchContext : ISearchContext
    {
        private INavigationContext NavigationContext { get; set; }
        private Reddit _reddit;
        public string Query { get { return _helper.Query; } set { _helper.Query = value; } }
        public string RestrictedToSubreddit { get { return _helper.RestrictedToSubreddit; } set { _helper.RestrictedToSubreddit = value; } }
        public bool SubredditsOnly { get { return _helper.SubredditsOnly; } set { _helper.SubredditsOnly = value; } }
        private string _after;
        private string _searchUri;
        private bool _hasLoaded = false;
        private SearchHelper _helper;
        public event Action ResultsInvalidated;

        public SearchContext(string query, string restrictedToSubreddit, bool subredditsOnly, Reddit reddit, INavigationContext navigationContext, OfflineService offline, ILinkContext linkContext, CoreDispatcher dispatcher)
        {
            NavigationContext = navigationContext;
            _reddit = reddit;
            LinkBuilderContext = new LinkBuilderContext { LinkContext = linkContext, Offline = offline, Reddit = _reddit, Subreddit = restrictedToSubreddit };
            _helper = new SearchHelper(() => { if (ResultsInvalidated != null) ResultsInvalidated(); }, triggeredQuery => { _searchUri = _after = null; _hasLoaded = false; if (ResultsInvalidated != null) ResultsInvalidated();  }, dispatcher, 3, "/", 1);
        }

        public bool HasAdditional
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Query) && (!_hasLoaded || !string.IsNullOrWhiteSpace(_after));
            }
        }

        public ILinkBuilderContext LinkBuilderContext { get; set; }

        public async Task<Listing> Load(IProgress<float> progress, CancellationToken token, bool ignoreCache)
        {
            _hasLoaded = true;
            var listing = await _reddit.Search(Query, null, SubredditsOnly, RestrictedToSubreddit, token, progress, ignoreCache);
            _searchUri = listing.Item1;
            _after = listing.Item2.Data.After;
            return listing.Item2;
        }

        public async Task<Listing> LoadAdditional(IProgress<float> progress, CancellationToken token)
        {
            var listing = await _reddit.GetAdditionalFromListing(_searchUri, _after, token, progress, true);
            _after = listing.Data.After;
            return listing;
        }

        public void GotoSubreddit(string url)
        {
            Navigation.GotoSubreddit(url, NavigationContext);
        }
    }

    public class SearchHelper
    {
        Action _defaultResults;
        Action<string> _startSearch;
        int _minimumCharCount;
        int _secondsBeforeSearch;
        string _alwaysSearchIfContains;
        DispatchTimer _dispatcher;
        public SearchHelper(Action defaultResults, Action<string> startSearch, CoreDispatcher dispatcher, int minimumCharCount, string alwaysSearchIfContains, int secondsBeforeSearch = 1)
        {
            _defaultResults = defaultResults;
            _startSearch = startSearch;
            _minimumCharCount = minimumCharCount;
            _alwaysSearchIfContains = alwaysSearchIfContains;
            _secondsBeforeSearch = secondsBeforeSearch;
            _dispatcher = new DispatchTimer(dispatcher);
        }

        private string _query;
        public string Query
        {
            get
            {
                return _query;
            }
            set
            {
                bool wasChanged = _query != value;
                if (wasChanged)
                {
                    _query = value;

                    if (_query.Length < _minimumCharCount)
                    {
                        _defaultResults();
                        RevokeQueryTimer();
                    }
                    else
                    {
                        RestartQueryTimer();
                    }
                }
            }
        }

        private string _restrictedToSubreddit;
        public string RestrictedToSubreddit
        {
            get
            {
                return _restrictedToSubreddit;
            }
            set
            {
                bool wasChanged = _restrictedToSubreddit != value;
                if (wasChanged)
                {
                    _restrictedToSubreddit = value;
                    RestartQueryTimer();
                }
            }
        }

        private bool _subredditsOnly;
        public bool SubredditsOnly
        {
            get
            {
                return _subredditsOnly;
            }
            set
            {
                bool wasChanged = _subredditsOnly != value;
                if (wasChanged)
                {
                    _subredditsOnly = value;
                    RestartQueryTimer();
                }
            }
        }

        Object _queryTimer;
        void RevokeQueryTimer()
        {
            if (_queryTimer != null)
            {
                _dispatcher.StopTimer(_queryTimer);
                _queryTimer = null;
            }
        }

        void RestartQueryTimer()
        {
            // Start or reset a pending query
            if (_queryTimer == null)
            {
                if (_secondsBeforeSearch > 0)
                    _queryTimer = _dispatcher.StartTimer(queryTimer_Tick, new TimeSpan(0, 0, _secondsBeforeSearch), true);
                else
                    queryTimer_Tick(null, null);
            }
            else
            {
                _dispatcher.StopTimer(_queryTimer);
                _dispatcher.RestartTimer(_queryTimer);
            }
        }

        void queryTimer_Tick(object sender, object timer)
        {
            // Stop the timer so it doesn't fire again unless rescheduled
            RevokeQueryTimer();

            if (!(_query != null && _query.Contains(_alwaysSearchIfContains)))
            {
                _startSearch(_query);
            }
        }
    }

    class SearchLinkContext : BaseLinkContext
    {
        public SearchViewModel SearchViewModel { get; set; }
        public override void GotoLink(LinkViewModel vm)
        {
            Navigation.GotoLink(SearchViewModel, vm.Thing.Url, NavigationContext);
        }
    }
}
