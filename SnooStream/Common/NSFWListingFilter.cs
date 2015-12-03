using SnooSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public interface IListingFilterContext
    {
        bool SettingsAllowOver18 { get; }
        bool SettingsAllowOver18Items { get; }
        Task<Subreddit> GetSubreddit(string name);
        Dictionary<string, bool> InitialFilter { get; }
    }

    public class NSFWListingFilter : IListingFilter
    {
        private IListingFilterContext Context { get; set; }
        CacheStore<Task<bool>> _nsfwParentCache = new CacheStore<Task<bool>>();
        public NSFWListingFilter(IListingFilterContext filterContext)
        {
            Context = filterContext;
        }

        public async Task<Listing> Filter(Listing listing)
        {
            if (listing != null && !Context.SettingsAllowOver18)
            {
                List<Thing> _removalList = new List<Thing>();
                foreach (var item in listing.Data.Children)
                {
                    if (item.Data is Link && ((Link)item.Data).Over18)
                    {
                        if (!Context.SettingsAllowOver18Items ||
                            (Context.InitialFilter.ContainsKey(((Link)item.Data).Subreddit) && Context.InitialFilter[((Link)item.Data).Subreddit]) ||
                            await _nsfwParentCache.GetOrCreate(((Link)item.Data).Subreddit, () => IsSubredditNSFW(((Link)item.Data).Subreddit)))
                        {
                            _removalList.Add(item);
                        }
                    }
                    else if (item.Data is Subreddit)
                    {
                        _nsfwParentCache.Add(((Subreddit)item.Data).DisplayName, Task.FromResult(((Subreddit)item.Data).Over18 ?? false));
                    }
                }

                if (_removalList.Count > 0)
                {
                    foreach (var thing in _removalList)
                    {
                        listing.Data.Children.Remove(thing);
                    }
                }
            }
            return listing;
        }

        private async Task<bool> IsSubredditNSFW(string subreddit)
        {
            try
            {
                var targetSubreddit = await Context.GetSubreddit(subreddit);
                if (targetSubreddit != null)
                {
                    return (targetSubreddit).Over18 ?? false;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                //_logger.Error("encountered exception while checking NSFW of a subreddit", ex);
                return false;
            }
        }
        internal Dictionary<string, bool> DumpState()
        {
            Dictionary<string, bool> result = new Dictionary<string, bool>(Context.InitialFilter);
            foreach (var element in _nsfwParentCache.Dump())
            {
                var elementResult = element.Value;
                if (elementResult != null && elementResult.IsCompleted && !result.ContainsKey(element.Key))
                {
                    result.Add(element.Key, elementResult.Result);
                }
            }
            return result;
        }

        public class CacheStore<T>
        {
            /// <summary>
            /// In-memory cache dictionary
            /// </summary>
            private Dictionary<string, T> _cache;
            private object _sync;


            /// <summary>
            /// Cache initializer
            /// </summary>
            public CacheStore()
            {
                _cache = new Dictionary<string, T>();
                _sync = new object();
            }

            /// <summary>
            /// Get an object from cache
            /// </summary>
            /// <typeparam name="T">Type of object</typeparam>
            /// <param name="key">Name of key in cache</param>
            /// <returns>Object from cache</returns>
            public T GetOrCreate(string key, Func<T> creator)
            {
                lock (_sync)
                {
                    if (_cache.ContainsKey(key) == false)
                        _cache.Add(key, creator());

                    return _cache[key];
                }
            }

            public void Add(string key, T value)
            {
                lock (_sync)
                {
                    if (!_cache.ContainsKey(key))
                    {
                        _cache.Add(key, value);
                    }
                }
            }

            public IEnumerable<KeyValuePair<string, T>> Dump()
            {
                lock (_sync)
                {
                    return _cache.ToArray();
                }
            }
        }
    }

    class ListingFilterContext : IListingFilterContext
    {
        public Reddit Reddit { get; set; }
        public OfflineService Offline { get; set; }
        public Dictionary<string, bool> InitialFilter { get; set; }
        public bool SettingsAllowOver18 { get; set; }
        public bool SettingsAllowOver18Items { get; set; }

        public async Task<Subreddit> GetSubreddit(string name)
        {
            var subredditThing = await Offline.GetSubreddit(name) ?? await Reddit.GetSubredditAbout(name, CancellationToken.None, new Progress<float>());
            return subredditThing?.Data as Subreddit;
        }
    }
}
