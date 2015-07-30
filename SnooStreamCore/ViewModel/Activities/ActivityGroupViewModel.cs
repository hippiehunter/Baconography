using GalaSoft.MvvmLight;
using SnooSharp;
using SnooStream.Common;
using SnooStream.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class ActivityGroupViewModel : ViewModelBase
    {
		private static string ReplyStatusIcon = "\uE172";
		private static string ReplyAllStatusIcon = "\uE165";
		private static string SoloPostedStatusIcon = "\uE11C";
		private static string SoloCommentStatusIcon = "\uE110";
		private static string SoloMessageStatusIcon = "\uE122";

        public class ActivityAggregate
        {
            ObservableCollection<ViewModelBase> _targetCollection;
            public bool HasItems
            {
                get
                {
                    return _targetCollection.Count > 0;
                }
            }
            public ActivityAggregate(INotifyCollectionChanged sourceCollection, ObservableCollection<ViewModelBase> targetCollection)
            {
                _targetCollection = targetCollection;
                sourceCollection.CollectionChanged += _groups_CollectionChanged;
            }

            void RegisterGroup(ActivityGroupViewModel group)
            {
                group.Activities.CollectionChanged += Activities_CollectionChanged;
                group.PropertyChanged += group_PropertyChanged;
            }

            void group_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "IsExpanded")
                {
                    var group = sender as ActivityGroupViewModel;
                    if (group.IsExpanded)
                    {
                        //need to make sure no one else is marked as expanded
                        //because we only allow one at a time
                        var existingExpanded = _targetCollection.FirstOrDefault(vm => vm != group && vm is ActivityGroupViewModel && ((ActivityGroupViewModel)vm).IsExpanded) as ActivityGroupViewModel;
                        if (existingExpanded != null)
                            existingExpanded.IsExpanded = false;

                        if (group.Activities.Count > 1)
                        {
                            var indexOfGroup = _targetCollection.IndexOf(group);
                            foreach (var activity in group.Activities)
                            {
                                _targetCollection.Insert(++indexOfGroup, activity);
                            }
                        }
                    }
                    else
                    {
                        //remove all of the activities from this group
                        if (group.Activities.Count > 1)
                        {
                            foreach (var item in group.Activities)
                                _targetCollection.Remove(item);
                        }
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
                        if (followingGroup != null)
                        {
                            _targetCollection.Insert(Math.Max(0, _targetCollection.IndexOf(followingGroup)), e.NewItems[0] as ActivityGroupViewModel);
                        }
                        else
                        {
                            _targetCollection.Add(e.NewItems[0] as ActivityGroupViewModel);
                        }

                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        var removedGroup = e.OldItems[0] as ActivityGroupViewModel;
                        UnregisterGroup(removedGroup);
                        _targetCollection.Remove(removedGroup);
                        if (removedGroup.IsExpanded)
                        {
                            foreach (var item in removedGroup.Activities)
                                _targetCollection.Remove(item);
                        }
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                        _targetCollection.Clear();
                        break;
                    default:
                        break;
                }
            }
        }

        public class ActivityAgeComparitor : IComparer<ActivityGroupViewModel>
        {
            public int Compare(ActivityGroupViewModel x, ActivityGroupViewModel y)
            {
                //invert the sort
                var result = y.CreatedUTC.CompareTo(x.CreatedUTC);
                if (result == 0 && ((ThingData)y.FirstActivity.GetThing().Data).Id != ((ThingData)x.FirstActivity.GetThing().Data).Id)
                    return 1;
                else
                    return result;
            }
        }

        public static string ProcessListing(ObservableSortedUniqueCollection<string, ActivityGroupViewModel> groups, Listing listing, string after, bool isMod = false)
        {
            if (listing != null)
            {
                foreach (var child in listing.Data.Children)
                {
                    ProcessThing(groups, child, isMod);
                }

                if (string.IsNullOrWhiteSpace(after))
                    return listing.Data.After;
            }
            return after;
        }

		public static void ProcessThing(ObservableSortedUniqueCollection<string, ActivityGroupViewModel> groups, Thing child, bool isMod = false)
		{
			var childName = ActivityViewModel.GetActivityGroupName(child);
			ActivityGroupViewModel existingGroup;
			if (groups.TryGetValue(childName, out existingGroup))
			{
				if (existingGroup.Activities.Count <= 1)
				{
					existingGroup.Merge(child);
				}
				else
					existingGroup.Merge(child);

				groups.Remove(childName);
				groups.Add(childName, existingGroup);
			}
			else
			{
                groups.Add(childName, ActivityGroupViewModel.MakeActivityGroup(childName, child, isMod));
			}
		}

        public static ActivityGroupViewModel MakeActivityGroup(string activityGroupName, Thing thing, bool isMod = false)
        {
            if (thing == null)
                throw new ArgumentNullException();

            var group = new ActivityGroupViewModel(activityGroupName, isMod);

            group.Merge(thing);
            return group;
        }

        public ActivityGroupViewModel(string activityGroupName, bool isMod)
        {
            IsMod = isMod;
            Id = activityGroupName;
            Activities = new ObservableSortedUniqueCollection<string, ActivityViewModel>(new ActivityViewModel.ActivityAgeComparitor());
        }

		public static string MakeActivityIdentifier(Thing thing)
		{
			var ident = "";
			if (thing.Data is Message)
			{
				var message = thing.Data as Message;
				ident += message.Author;
				ident += message.Body.GetHashCode();
				ident += message.CreatedUTC;
			}
			else
				ident = ((ThingData)thing.Data).Name;
			return ident;
		}

        public void Merge(Thing additional)
        {
            var currentFirstActivity = FirstActivity;

			var thingName = MakeActivityIdentifier(additional);
            if (!Activities.ContainsKey(thingName))
            {
                var targetActivity = ActivityViewModel.CreateActivity(additional);

                if (Activities.Count == 0)
                {
                    FirstActivity = targetActivity;
                    Activities.Add(thingName, targetActivity);
                    if (IsMod)
                    {
                        if(targetActivity is PostedLinkActivityViewModel)
                        {
                            foreach (var modAction in ((Link)targetActivity.GetThing().Data).ModReports)
                            {
                                IsConversation = true;
                                Activities.Add(modAction[1], new ReportActivityViewModel(modAction, true));
                            }
                            foreach (var modAction in ((Link)targetActivity.GetThing().Data).UserReports)
                            {
                                IsConversation = true;
                                Activities.Add(modAction[1], new ReportActivityViewModel(modAction, false));
                            }
                        }
                        else if(targetActivity is PostedCommentActivityViewModel)
                        {
                            foreach (var modAction in ((Comment)targetActivity.GetThing().Data).ModReports)
                            {
                                IsConversation = true;
                                Activities.Add(modAction[1], new ReportActivityViewModel(modAction, true));
                            }
                            foreach (var modAction in ((Comment)targetActivity.GetThing().Data).UserReports)
                            {
                                IsConversation = true;
                                Activities.Add(modAction[1], new ReportActivityViewModel(modAction, false));
                            }
                        }
                    }
                }
                else
                {
                    Activities.Add(thingName, targetActivity);
                    if (!IsConversation)
                    {
                        IsConversation = true;
                        RaisePropertyChanged("IsConversation");
                        FirstActivity = ((IEnumerable<ActivityViewModel>)Activities).First();
                    }
                }
            }

            Status = "";

            if (FirstActivity.IsSelf && !IsMod)
			{
				var betterFirstActivity = ((IEnumerable<ActivityViewModel>)Activities).FirstOrDefault(activity => !activity.IsSelf);
				if (betterFirstActivity != null)
				{
					if (betterFirstActivity is MessageActivityViewModel || betterFirstActivity is ModeratorMessageActivityViewModel)
						Status = ReplyStatusIcon;
					else
						Status = ReplyAllStatusIcon;

                    FirstActivity = betterFirstActivity;
                    ActivityViewModel.FixupFirstActivity(FirstActivity, Activities);
                    Title = FirstActivity.Title;
				}
				else
				{
                    ActivityViewModel.FixupFirstActivity(FirstActivity, Activities);
                    if (FirstActivity is PostedLinkActivityViewModel)
					{
                        Title = ((PostedLinkActivityViewModel)FirstActivity).Subreddit;
						Status = SoloPostedStatusIcon;
					}
                    else if (FirstActivity is PostedCommentActivityViewModel)
					{
                        Title = ((PostedCommentActivityViewModel)FirstActivity).Subreddit;
						Status = SoloCommentStatusIcon;
					}
                    else if (FirstActivity is RecivedCommentReplyActivityViewModel)
					{
                        Title = ((RecivedCommentReplyActivityViewModel)FirstActivity).Subreddit;
					}
                    else if (FirstActivity is MessageActivityViewModel)
					{
						Status = SoloMessageStatusIcon;
                        Title = ((MessageActivityViewModel)FirstActivity).Destination;
					}
				}
			}
			else
			{
                ActivityViewModel.FixupFirstActivity(FirstActivity, Activities);
                Title = FirstActivity.Title;
			}

            if (FirstActivity is PostedCommentActivityViewModel)
                IsComment = true;

            CreatedUTC = FirstActivity.CreatedUTC;
            PreviewBody = FirstActivity.PreviewBody;
            SubTitle = FirstActivity.SubTitle;
            HasUnviewed = Activities.Values.Any(vm => vm.IsNew);

            RaisePropertyChanged("CreatedUTC");
			RaisePropertyChanged("PreviewBody");
			RaisePropertyChanged("Title");
			RaisePropertyChanged("SubTitle");
            RaisePropertyChanged("IsComment");
            RaisePropertyChanged("FirstActivity");
            RaisePropertyChanged("HasUnviewed");
        }
        private bool IsMod { get; set; }
        public string Id { get; set; }
        public DateTime CreatedUTC {get; protected set;}
		public string Title { get; protected set; }
		public string SubTitle { get; protected set; }
		public string PreviewBody { get; protected set; }
		public string Status { get; protected set; }
        public bool IsComment { get; protected set; }
        public bool HasUnviewed { get; set; }
        public ActivityViewModel FirstActivity { get; set; }

        private bool _isExpanded;
        public bool IsExpanded 
        {
            get { return _isExpanded; }
            set { _isExpanded = value; RaisePropertyChanged("IsExpanded"); }
        }
        public bool IsConversation { get; protected set; }
        public ObservableSortedUniqueCollection<string, ActivityViewModel> Activities { get; protected set; }

        internal void DumpThings(List<Thing> things)
        {
            if (Activities.Count > 0)
            {
                foreach (var activity in Activities)
                {
                    things.Add(activity.GetThing());
                }
            }
            else
            {
                things.Add(FirstActivity.GetThing());
            }
        }

        public void Tapped()
        {
            if (IsConversation)
                IsExpanded = !IsExpanded;
            else
            {
                //nav to detailed activity page
                FirstActivity.Tapped();
            }
        }
    }
}
