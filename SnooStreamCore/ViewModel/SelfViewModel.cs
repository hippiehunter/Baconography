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
        public class SelfActivityAggregate : PortableObservableCollection<ViewModelBase>
        {
            ObservableSortedUniqueCollection<string, ActivityGroupViewModel> _groups;
            public SelfActivityAggregate(ObservableSortedUniqueCollection<string, ActivityGroupViewModel> groups, Func<Task> loadMore) : base(loadMore)
            {
                _groups = groups;
                _groups.CollectionChanged += _groups_CollectionChanged;
            }

            void RegisterGroup(ActivityGroupViewModel group)
            {
                group.Activities.CollectionChanged += Activities_CollectionChanged;
                group.PropertyChanged += group_PropertyChanged;
            }

            void group_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if(e.PropertyName == "IsExpanded")
                {
                    var group = sender as ActivityGroupViewModel;
                    if(group.IsExpanded && group.Activities.Count > 1)
                    {
                        //need to make sure no one else is marked as expanded
                        //because we only allow one at a time
                        var existingExpanded = this.FirstOrDefault(vm => vm != group && vm is ActivityGroupViewModel && ((ActivityGroupViewModel)vm).IsExpanded) as ActivityGroupViewModel;
                        if (existingExpanded != null)
                            existingExpanded.IsExpanded = false;

                        var indexOfGroup = IndexOf(group);
                        foreach(var activity in group.Activities)
                        {
                            Insert(++indexOfGroup, activity);
                        }
                    }
                    else
                    {
                        //remove all of the activities from this group
                        foreach (var item in group.Activities)
                            Remove(item);
                    }
                }
            }

            void Activities_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {

                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                        break;
                    default:
                        break;
                }
            }

            void UnregisterGroup(ActivityGroupViewModel group)
            {
                group.Activities.CollectionChanged -= Activities_CollectionChanged;
                group.PropertyChanged -= group_PropertyChanged;
            }

            void _groups_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {
                var collection = sender as ObservableSortedUniqueCollection<string, ActivityGroupViewModel>;
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        RegisterGroup(e.NewItems[0] as ActivityGroupViewModel);
                        var followingGroup = collection.GetElementFollowing(e.NewItems[0] as ActivityGroupViewModel);
                        if(followingGroup != null)
                        {
                            Insert(Math.Max(0, IndexOf(followingGroup) - 1), e.NewItems[0] as ActivityGroupViewModel);
                        }
                        else
                        {
                            Add(e.NewItems[0] as ActivityGroupViewModel);
                        }
                        
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        var removedGroup = e.OldItems[0] as ActivityGroupViewModel;
                        UnregisterGroup(removedGroup);
                        if(removedGroup.IsExpanded)
                        {
                            foreach (var item in removedGroup.Activities)
                                Remove(item);
                        }
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                        Clear();
                        break;
                    default:
                        break;
                }
            }
        }

        public SelfViewModel(IEnumerable<Thing> initialThings, string oldestMessage, string oldestSentMessage, string oldestActivity)
        {
            //load up the activities
            Groups = new ObservableSortedUniqueCollection<string, ActivityGroupViewModel>(new ActivityGroupViewModel.ActivityAgeComparitor());
            Activities = new SelfActivityAggregate(Groups, () => PullNew());
            foreach (var thing in initialThings)
            {
                var thingName = ActivityViewModel.GetActivityGroupName(thing);
                ActivityGroupViewModel existingGroup;
                if (Groups.TryGetValue(thingName, out existingGroup))
                {
                    existingGroup.Merge(thing);
                }
                else
                {
                    Groups.Add(thingName, ActivityGroupViewModel.MakeActivityGroup(thing));
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
            Groups.Clear();
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
        public ObservableSortedUniqueCollection<string, ActivityGroupViewModel> Groups { get; private set; }
        public SelfActivityAggregate Activities { get; private set; }
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
                    if (Groups.TryGetValue(childName, out existingGroup))
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
                        Groups.Add(childName, ActivityGroupViewModel.MakeActivityGroup(child));
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
                SelfThings = new List<Thing>(DumpThings(Groups)),
                AfterSelfAction = OldestActivity,
                AfterSelfMessage = OldestMessage,
                AfterSelfSentMessage = OldestSentMessage
            };
        }
    }
}
