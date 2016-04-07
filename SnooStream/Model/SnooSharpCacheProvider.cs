using SnooSharp;
using SnooStream.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Model
{
    class SnooSharpCacheProvider : ICachingProvider
    {
        HashSet<string> _highValueListingUrls;
        Dictionary<string, Listing> _listingLookup = new Dictionary<string, Listing>();
        SortedList<DateTime, object> _listingTimeoutLookup = new SortedList<DateTime, object>();

        Dictionary<string, Thing> _thingLookup = new Dictionary<string, Thing>();
        SortedList<DateTime, object> _thingTimeoutLookup = new SortedList<DateTime, object>();

        OfflineService _offlineService;
        bool _hasChanges = false;
        string _currentUser = "";


        public SnooSharpCacheProvider(OfflineService offlineService, IEnumerable<string> highValueListingUrls)
        {
            _highValueListingUrls = new HashSet<string>(highValueListingUrls);
            _offlineService = offlineService;
            PeriodicTask.DefaultTask.AddTask(() => _hasChanges, DumpState);
        }

        public async Task LoadStateForUser(string currentUser)
        {
            //maybe we should register listeners for listings
            //would be usefull to trigger auto refresh via this mechanism
            //switching users we need to purge any old listing state we might have

            List<string> invalidationList = new List<string>();

            if (currentUser != _currentUser)
            {
                foreach (var url in _highValueListingUrls)
                {
                    if (_listingLookup.ContainsKey(url))
                    {
                        invalidationList.Add(url);
                        _listingLookup.Remove(url);
                    }
                }
            }

            _currentUser = currentUser;

            var foundBlobs = await _offlineService.MaybeRetrieveBlobs<Listing>(_highValueListingUrls.Select(url => currentUser + ":" + url), TimeSpan.FromDays(28));
            foreach (var foundBlob in foundBlobs)
            {
                var targetKey = foundBlob.Key.Substring(foundBlob.Key.IndexOf(':') + 1);
                if (_listingLookup.ContainsKey(targetKey))
                    _listingLookup[targetKey] = foundBlob.Value;
                else
                    _listingLookup.Add(targetKey, foundBlob.Value);
            }
            _listingLookup = foundBlobs; //these dont have timeout values and should never be removed
        }

        private async Task DumpState()
        {
            _hasChanges = false;
            var highValueListings = _listingLookup.Where(kvp => _highValueListingUrls.Contains(kvp.Key)).ToDictionary(kvp => _currentUser + ":" + kvp.Key, kvp => kvp.Value);
            await _offlineService.StoreBlobs(highValueListings);
        }

        public Task<Listing> GetListing(string url)
        {
            Listing result;
            if (_listingLookup.TryGetValue(url.ToLower(), out result))
                return Task.FromResult(result);
            else
                return Task.FromResult<Listing>(null);
        }

        public Task<Thing> GetThingById(string id)
        {
            Thing thing;
            if (_thingLookup.TryGetValue(id.ToLower(), out thing))
                return Task.FromResult(thing);
            else
                return Task.FromResult<Thing>(null);
        }

        public Task SetListing(string url, Listing listing)
        {
            if (_highValueListingUrls.Contains(url))
                _hasChanges = true;

            var timeAdded = DateTime.UtcNow;
            listing.DataAge = timeAdded;
            if (_listingLookup.Count > 25)
            {
                var evicted = _listingTimeoutLookup.Take(15).ToList();
                foreach (var key in evicted)
                {
                    if(key.Value is string)
                        _listingLookup.Remove(key.Value as string);
                    else
                    {
                        foreach (var item in key.Value as List<string>)
                        {
                            _listingLookup.Remove(item);
                        }
                    }
                    _listingTimeoutLookup.Remove(key.Key);
                }
            }

            var lowerUrl = url.ToLower();
            if (_listingLookup.ContainsKey(lowerUrl))
            {
                _listingLookup[lowerUrl] = listing;
                var timeoutIndex = _listingTimeoutLookup.IndexOfValue(lowerUrl);
                if (timeoutIndex > -1)
                    _listingTimeoutLookup.RemoveAt(timeoutIndex);
            }
            else
            {
                _listingLookup.Add(lowerUrl, listing);
            }


            object timeoutBlob;
            if (_listingTimeoutLookup.TryGetValue(timeAdded, out timeoutBlob))
            {
                if (timeoutBlob is List<string>)
                    ((List<string>)timeoutBlob).Add(lowerUrl);
                else
                    _listingTimeoutLookup[timeAdded] = new List<string> { timeoutBlob as string, lowerUrl };
            }
            else
                _listingTimeoutLookup.Add(timeAdded, lowerUrl);

            ProcessListingForSingles(listing);

            return Task.CompletedTask;
        }

        public Task SetThing(Thing thing)
        {
            var thingData = thing.Data as ThingData;
            if (thingData != null)
            {
                AddThing(thingData.Id, thing);
            }

            if (thing.Data is Link)
            {
                AddThing(((Link)thing.Data).Permalink.ToLower(), thing);
            }

            return Task.CompletedTask;
        }

        private void AddThing(string key, Thing thing)
        {
            var timeAdded = DateTime.UtcNow;
            thing.DataAge = timeAdded;
            if (_thingLookup.Count > 5000)
            {
                var evicted = _listingTimeoutLookup.Take(2500).ToList();
                foreach (var evictedKey in evicted)
                {
                    if (evictedKey.Value is string)
                        _thingLookup.Remove(evictedKey.Value as string);
                    else
                    {
                        foreach (var item in evictedKey.Value as List<string>)
                        {
                            _thingLookup.Remove(item);
                        }
                    }
                    _listingTimeoutLookup.Remove(evictedKey.Key);
                }
            }

            if (_thingLookup.ContainsKey(key))
            {
                _thingLookup[key] = thing;
            }
            else
            {
                _thingLookup.Add(key, thing);
            }

            object timeoutBlob;
            if (_thingTimeoutLookup.TryGetValue(timeAdded, out timeoutBlob))
            {
                if (timeoutBlob is List<string>)
                    ((List<string>)timeoutBlob).Add(key);
                else
                    _thingTimeoutLookup[timeAdded] = new List<string> { timeoutBlob as string, key };
            }
            else
                _thingTimeoutLookup.Add(timeAdded, key);
        }

        private void ProcessListingForSingles(Listing listing)
        {
            foreach (var thing in listing.Data.Children)
            {
                if (thing.Data is Link)
                    SetThing(thing);
                else if (thing.Data is Subreddit)
                    SetThing(thing);
            }
        }
    }
}
