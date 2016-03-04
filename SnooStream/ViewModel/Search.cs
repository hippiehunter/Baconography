using SnooSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public interface ISearchContext
    {
        bool HasAdditional { get; }
        Task<Listing> Load(IProgress<float> progress, CancellationToken token, bool ignoreCache);
        Task<Listing> LoadAdditional(IProgress<float> progress, CancellationToken token);
        string Query { get; set; }
        string RestrictedToSubreddit { get; set; }
        bool SubredditsOnly { get; set; }
    }

    public class SearchViewModel
    {
        private ISearchContext searchContext;

        public SearchViewModel(ISearchContext searchContext)
        {
            this.searchContext = searchContext;
        }

        public string Query { get; set; }
        public string RestrictedToSubreddit { get; set; }
        public bool SubredditsOnly { get; set; }
    }

    public class SearchContext : ISearchContext
    {
        private Reddit _reddit;
        public string Query { get; set; }
        public string RestrictedToSubreddit { get; set; }
        public bool SubredditsOnly { get; set; }
        private string _after;
        private string _searchUri;
        private bool _hasLoaded = false;
        public SearchContext(string query, string restrictedToSubreddit, bool subredditsOnly, Reddit reddit)
        {
            _reddit = reddit;
        }

        public bool HasAdditional
        {
            get
            {
                return !_hasLoaded || !string.IsNullOrWhiteSpace(_after);
            }
        }

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
    }
}
