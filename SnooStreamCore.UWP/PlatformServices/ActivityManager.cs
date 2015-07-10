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
			CanStore = true;
        }

		public void Clear()
		{
			_activityManager.ClearState();
		}

        static Listing empty = new Listing { Data = new ListingData { Children = new List<Thing>() } };

        public Listing Activity
        {
            get
            {
                try
                {
                    return _activityManager.ActivityBlob != null ?
                        JsonConvert.DeserializeObject<Listing>(_activityManager.ActivityBlob) :
                        empty;
                }
                catch
                {
                    return empty;
                }
            }
        }

        public Listing Received
        {
            get
            {
                try
                {
                    return _activityManager.ReceivedBlob != null ?
                        JsonConvert.DeserializeObject<Listing>(_activityManager.ReceivedBlob.Replace("\"kind\": \"t1\"", "\"kind\": \"t4\"")) :
                        empty;
                }
                catch
                {
                    return empty;
                }
            }
        }

        public Listing Sent
        {
            get
            {
                try
                {
                    return _activityManager.SentBlob != null ?
                        JsonConvert.DeserializeObject<Listing>(_activityManager.SentBlob) :
                        empty;
                }
                catch
                {
                    return empty;
                }
            }
        }

        public string OAuth
        {
            set
            {
                _oAuthBlob = value;
            }
        }

		public bool CanStore { get; set; }

        public bool NeedsRefresh(bool appStart)
        {
			if (appStart && _activityManager.UpdateCountSinceActivity < 5)
				return true;
			else
				return _activityManager.NeedsRefresh;
        }

        public async Task Refresh()
        {
            await SnooStreamViewModel.NotificationService.Report("refreshing activity", async () =>
            {
                await Task.Run(async () =>
                    {
                        await _activityManager.Refresh(_oAuthBlob, (toast, obj) =>
                        {
#if WINDOWS_PHONE_APP
                            //tag is the item name, use it to navigate directly to the response chain
                            //toast.Tag
#endif

						}, CanStore);
						if(SnooStreamViewModel.RedditUserState.Username != null)
						{
							var currentAccount = await SnooStreamViewModel.RedditService.GetIdentity();
							SnooStreamViewModel.RedditUserState.IsMod = currentAccount.IsMod;
							SnooStreamViewModel.RedditUserState.IsGold = currentAccount.IsGold;
						}
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
