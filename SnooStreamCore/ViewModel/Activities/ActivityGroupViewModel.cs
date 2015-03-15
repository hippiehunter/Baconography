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
        public class SelfActivityAggregate
        {
            ObservableCollection<ViewModelBase> _targetCollection;
            public bool HasItems
            {
                get
                {
                    return _targetCollection.Count > 0;
                }
            }
            public SelfActivityAggregate(INotifyCollectionChanged sourceCollection, ObservableCollection<ViewModelBase> targetCollection)
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

        public static string ProcessListing(ObservableSortedUniqueCollection<string, ActivityGroupViewModel> groups, Listing listing, string after)
        {
            if (listing != null)
            {
                foreach (var child in listing.Data.Children)
                {
					ProcessThing(groups, child);
                }

                if (string.IsNullOrWhiteSpace(after))
                    return listing.Data.After;
            }
            return after;
        }

		public static void ProcessThing(ObservableSortedUniqueCollection<string, ActivityGroupViewModel> groups, Thing child)
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
				groups.Add(childName, ActivityGroupViewModel.MakeActivityGroup(childName, child));
			}
		}

        public static ActivityGroupViewModel MakeActivityGroup(string activityGroupName, Thing thing)
        {
            if (thing == null)
                throw new ArgumentNullException();

            var group = new ActivityGroupViewModel(activityGroupName);

            group.Merge(thing);
            return group;
        }

        public ActivityGroupViewModel(string activityGroupName)
        {
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
                    _innerFirstActivity = targetActivity;
                    _innerFirstActivityName = thingName;
                    Activities.Add(thingName, targetActivity);
                }
                else if (Activities.Count > 0)
                    Activities.Add(thingName, targetActivity);

                if (!IsConversation && Activities.Count > 2)
                {
                    IsConversation = true;
                    RaisePropertyChanged("IsConversation");
                }
            }

            CreatedUTC = FirstActivity.CreatedUTC;

            ActivityViewModel.FixupFirstActivity(FirstActivity, Activities);

            if (currentFirstActivity != FirstActivity)
                RaisePropertyChanged("FirstActivity");
        }
        public string Id { get; set; }
        public DateTime CreatedUTC {get; protected set;}
        private ActivityViewModel _innerFirstActivity;
        private string _innerFirstActivityName;
        public ActivityViewModel FirstActivity 
        {
            get
            {
                if (Activities.Count == 0)
                    return _innerFirstActivity;
                else
                    return ((IEnumerable<ActivityViewModel>)Activities).First();
            }
        }

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
                things.Add(_innerFirstActivity.GetThing());
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
