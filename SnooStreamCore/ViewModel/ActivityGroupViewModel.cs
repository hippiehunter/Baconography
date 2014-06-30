using GalaSoft.MvvmLight;
using SnooSharp;
using SnooStream.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace SnooStream.ViewModel
{
    public class ActivityGroupViewModel : ViewModelBase
    {
        public class ActivityAgeComparitor : IComparer<ActivityGroupViewModel>
        {
            public int Compare(ActivityGroupViewModel x, ActivityGroupViewModel y)
            {
                //invert the sort
                return y.CreatedUTC.CompareTo(x.CreatedUTC);
            }
        }

        public static ActivityGroupViewModel MakeActivityGroup(Thing thing)
        {
            if (thing == null)
                throw new ArgumentNullException();

            var group = new ActivityGroupViewModel();

            group.Merge(thing);
            return group;
        }

        public ActivityGroupViewModel()
        {
            Activities = new ObservableSortedUniqueCollection<string, ActivityViewModel>(new ActivityViewModel.ActivityAgeComparitor());
        }

        public void Merge(Thing additional)
        {
            var currentFirstActivity = Activities.Count > 0 ? FirstActivity : null;

            var thingName = ((ThingData)additional.Data).Name;
            if (!Activities.ContainsKey(thingName))
            {
                var targetActivity = ActivityViewModel.CreateActivity(additional);

                if (Activities.Count == 0 && _innerFirstActivity == null)
                {
                    _innerFirstActivity = targetActivity;
                    _innerFirstActivityName = thingName;
                }
                else if (Activities.Count == 0 && _innerFirstActivityName != thingName)
                {
                    Activities.Add(_innerFirstActivityName, _innerFirstActivity);
                    Activities.Add(thingName, targetActivity);
                }
                else if (Activities.Count > 0)
                    Activities.Add(thingName, targetActivity);

                if (!IsConversation && Activities.Count > 1)
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

        public bool IsExpanded { get; set; }
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
    }
}
