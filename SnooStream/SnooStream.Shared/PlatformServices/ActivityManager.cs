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
                return _activityManager.SentBlob != null ?
                    JsonConvert.DeserializeObject<Listing>(_activityManager.SentBlob) :
                    new Listing { Data = new ListingData { Children = new List<Thing>() } };
            }
        }

        public Listing Received
        {
            get
            {
                return _activityManager.ReceivedBlob != null ?
                    JsonConvert.DeserializeObject<Listing>(_activityManager.ReceivedBlob) :
                    new Listing { Data = new ListingData { Children = new List<Thing>() } };
            }
        }

        public Listing Sent
        {
            get
            {
                return _activityManager.ActivityBlob != null ?
                    JsonConvert.DeserializeObject<Listing>(_activityManager.ActivityBlob) :
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
    }
}
