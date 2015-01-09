using SnooStream.Services;
using System;
using System.Collections.Generic;
using System.Text;
using SnooSharp;
using System.Threading.Tasks;
using SnooStream.ViewModel;
using Newtonsoft.Json;

namespace SnooStream.PlatformServices
{
    class ActivityManager : IActivityManager
    {
        SnooStreamBackground.ActivityManager _activityManager = new SnooStreamBackground.ActivityManager();
        string _oAuthBlob;
        public ActivityManager()
        {
        }

        public Listing Activity
        {
            get
            {
                return _activityManager.ActivityBlob != null ?
                    JsonConvert.DeserializeObject<Listing>(_activityManager.ActivityBlob) :
                    new Listing { Data = new ListingData { Children = new List<Thing>() } };
            }
        }

        public Listing Received
        {
            get
            {
                return _activityManager.ReceivedBlob != null ?
                    JsonConvert.DeserializeObject<Listing>(_activityManager.ReceivedBlob.Replace("\"kind\": \"t1\"", "\"kind\": \"t4\"")) :
                    new Listing { Data = new ListingData { Children = new List<Thing>() } };
            }
        }

        public Listing Sent
        {
            get
            {
                return _activityManager.SentBlob != null ?
                    JsonConvert.DeserializeObject<Listing>(_activityManager.SentBlob) :
                    new Listing { Data = new ListingData { Children = new List<Thing>() } };
            }
        }

        public string OAuth
        {
            set
            {
                _oAuthBlob = value;
            }
        }

        public bool NeedsRefresh()
        {
            return _activityManager.NeedsRefresh;
        }

        public async Task Refresh()
        {
            await SnooStreamViewModel.NotificationService.Report("refreshing activity", async () =>
            {
                await _activityManager.Refresh(_oAuthBlob, (toast, obj) =>
                {
#if WINDOWS_PHONE_APP
                    //tag is the item name, use it to navigate directly to the response chain
                    //toast.Tag
#endif

                });
            });
            
        }


        public Listing ContextForId(string id)
        {
            var result = _activityManager.ContextForId(id);
            if (string.IsNullOrWhiteSpace(result))
                return new Listing { Data = new ListingData { Children = new List<Thing>() } };
            else
            {
                var resultList = new List<Thing>();
                var resultListing = new Listing { Data = new ListingData { Children = resultList } };
                var listings = JsonConvert.DeserializeObject<Listing[]>(result);
                foreach(var listing in listings)
                {
                    resultList.AddRange(listing.Data.Children);
                    if (listing.Data.After != null)
                        resultListing.Data.After = listing.Data.After;

                    if (listing.Data.Before != null)
                        resultListing.Data.Before = listing.Data.Before;
                }
                return resultListing;
            }
        }
    }
}
