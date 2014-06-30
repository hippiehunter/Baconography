using GalaSoft.MvvmLight;
using SnooSharp;
using SnooStream.Common;
using SnooStream.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class SelfViewModel : ViewModelBase
    {
        public SelfViewModel(IEnumerable<Thing> initialThings, string oldestMessage, string oldestSentMessage, string oldestActivity)
        {
            //load up the activities
            Activities = new ObservableSortedUniqueCollection<string, ActivityGroupViewModel>(new ActivityGroupViewModel.ActivityAgeComparitor());
            foreach (var thing in initialThings)
            {
                var thingName = ActivityViewModel.GetActivityGroupName(thing);
                ActivityGroupViewModel existingGroup;
                if (Activities.TryGetValue(thingName, out existingGroup))
                {
                    existingGroup.Merge(thing);
                }
                else
                {
                    Activities.Add(thingName, ActivityGroupViewModel.MakeActivityGroup(thing));
                }
            }

            OldestMessage = oldestMessage;
            OldestSentMessage = oldestSentMessage;
            OldestActivity = oldestActivity;

            MessengerInstance.Register<UserLoggedInMessage>(this, OnUserLoggedIn);
        }

        private async void OnUserLoggedIn(UserLoggedInMessage obj)
        {
            RaisePropertyChanged("IsLoggedIn");
            RaisePropertyChanged("Activities");
            Activities.Clear();
            await PullNew();
        }

        public bool IsLoggedIn
        {
            get
            {
                return !String.IsNullOrWhiteSpace(SnooStreamViewModel.RedditService.CurrentUserName);
            }
        }

        public static IEnumerable<Thing> DumpThings(IEnumerable<ActivityGroupViewModel> activities)
        {
            List<Thing> things = new List<Thing>();
            foreach(var group in activities)
            {
                group.DumpThings(things);
            }
            return things;
        }

        private string OldestMessage { get; set; }
        private string OldestSentMessage { get; set; }
        private string OldestActivity { get; set; }
        public ObservableSortedUniqueCollection<string, ActivityGroupViewModel> Activities { get; private set; }
        public async Task PullNew()
        {
            if (!IsLoggedIn)
                throw new InvalidOperationException("User must be logged in to do this");

            Listing inbox = null;
            Listing outbox = null;
            Listing activity = null;

            await SnooStreamViewModel.NotificationService.Report("refreshing inbox", async () =>
                {
                    inbox = await SnooStreamViewModel.RedditService.GetMessages(null);
                });

            OldestMessage = ProcessListing(inbox, OldestMessage);

            await SnooStreamViewModel.NotificationService.Report("refreshing outbox", async () =>
                {
                    outbox = await SnooStreamViewModel.RedditService.GetSentMessages(null);
                });

            OldestSentMessage = ProcessListing(outbox, OldestSentMessage);

            await SnooStreamViewModel.NotificationService.Report("refreshing activity", async () =>
                {
                    activity = await SnooStreamViewModel.RedditService.GetPostsByUser(SnooStreamViewModel.RedditService.CurrentUserName, null);
                });

            OldestActivity = ProcessListing(activity, OldestActivity);
        }

        private string ProcessListing(Listing listing, string after)
        {
            if (listing != null)
            {
                foreach (var child in listing.Data.Children)
                {
                    var childName = ActivityViewModel.GetActivityGroupName(child);
                    ActivityGroupViewModel existingGroup;
                    if (Activities.TryGetValue(childName, out existingGroup))
                    {
                        if (existingGroup.Activities.Count <= 1)
                        {
                            existingGroup.Merge(child);
                        }
                        else
                            existingGroup.Merge(child);
                    }
                    else
                    {
                        Activities.Add(childName, ActivityGroupViewModel.MakeActivityGroup(child));
                    }
                }

                if (string.IsNullOrWhiteSpace(after))
                    return listing.Data.After;
            }
            return after;
        }

        public async Task PullOlder()
        {
            if (!IsLoggedIn)
                throw new InvalidOperationException("User must be logged in to do this");

            Listing inbox = null;
            Listing outbox = null;
            Listing activity = null;

            if (!string.IsNullOrWhiteSpace(OldestMessage))
            {
                await SnooStreamViewModel.NotificationService.Report("getting additional inbox", async () =>
                {
                    inbox = await SnooStreamViewModel.RedditService.GetAdditionalFromListing(string.Format(Reddit.MailBaseUrlFormat, "inbox"), OldestMessage, null);
                });

                OldestMessage = ProcessListing(inbox, OldestMessage);
            }

            if (!string.IsNullOrWhiteSpace(OldestSentMessage))
            {
                await SnooStreamViewModel.NotificationService.Report("getting additional outbox", async () =>
                {
                    outbox = await SnooStreamViewModel.RedditService.GetAdditionalFromListing(string.Format(Reddit.MailBaseUrlFormat, "sent"), OldestSentMessage, null);
                });

                OldestSentMessage = ProcessListing(inbox, OldestSentMessage);
            }


            if (!string.IsNullOrWhiteSpace(OldestActivity))
            {
                await SnooStreamViewModel.NotificationService.Report("getting additional activity", async () =>
                {
                    activity = await SnooStreamViewModel.RedditService.GetAdditionalFromListing(string.Format(Reddit.PostByUserBaseFormat, SnooStreamViewModel.RedditService.CurrentUserName), OldestActivity, null);
                });

                OldestActivity = ProcessListing(inbox, OldestActivity);
            }
        }

        internal SelfInit Dump()
        {
            return new SelfInit
            {
                SelfThings = new List<Thing>(DumpThings(Activities)),
                AfterSelfAction = OldestActivity,
                AfterSelfMessage = OldestMessage,
                AfterSelfSentMessage = OldestSentMessage
            };
        }
    }
}
