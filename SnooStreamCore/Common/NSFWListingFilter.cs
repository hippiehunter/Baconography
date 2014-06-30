using SnooSharp;
using SnooStream.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public class NSFWListingFilter : IListingFilter
    {
        Settings _settings;
        OfflineService _offlineService;
        Reddit _redditService;
        CacheStore<Task<bool>> _nsfwParentCache = new CacheStore<Task<bool>>();
        Dictionary<string, bool> _initialFilter;

        public void Initialize(Settings settings, OfflineService offlineService, Reddit redditService, Dictionary<string, bool> initialFilter)
        {
            _settings = settings;
            _initialFilter = initialFilter;
            _offlineService = offlineService;
            _redditService = redditService;
        }


        public async Task<Listing> Filter(Listing listing)
        {
            if (listing != null && !_settings.AllowOver18)
            {
                List<Thing> _removalList = new List<Thing>();
                foreach (var item in listing.Data.Children)
                {
                    if (item.Data is Link && ((Link)item.Data).Over18)
                    {
                        if (!_settings.AllowOver18Items ||
                            (_initialFilter.ContainsKey(((Link)item.Data).Subreddit) && _initialFilter[((Link)item.Data).Subreddit]) ||
                            await _nsfwParentCache.GetOrCreate(((Link)item.Data).Subreddit, () => IsSubredditNSFW(((Link)item.Data).Subreddit)))
                        {
                            _removalList.Add(item);
                        }
                    }
                    else if (item.Data is Subreddit)
                    {
                        _nsfwParentCache.Add(((Subreddit)item.Data).DisplayName, Task.FromResult(((Subreddit)item.Data).Over18));
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
                var targetSubreddit = await _offlineService.GetSubreddit(subreddit) ?? await _redditService.GetSubreddit(subreddit);
                if (targetSubreddit != null && targetSubreddit.Data is Subreddit)
                {
                    return ((Subreddit)targetSubreddit.Data).Over18;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("encountered exception while checking NSFW of a subreddit {0}", ex);
                return false;
            }
        }
        internal Dictionary<string, bool> DumpState()
        {
            Dictionary<string, bool> result = new Dictionary<string, bool>(_initialFilter);
            foreach (var element in _nsfwParentCache.Dump())
            {
                var elementResult = element.Value.TryValueS();
                if (elementResult != null && !result.ContainsKey(element.Key))
                {
                    result.Add(element.Key, elementResult.Value);
                }
            }
            return result;
        }
    }
}
