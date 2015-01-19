using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SnooSharp;
using SnooStream.Common;
using SnooStream.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class AboutUserViewModel : ViewModelBase, IRefreshable
    {
        public class UserActivityLoader : IIncrementalCollectionLoader<ViewModelBase>
        {
            AboutUserViewModel _user;
            ActivityGroupViewModel.SelfActivityAggregate _activityAggregate;
            bool _hasLoaded;
            public UserActivityLoader(AboutUserViewModel user)
            {
                _hasLoaded = false;
                _user = user;
            }

            public Task AuxiliaryItemLoader(IEnumerable<ViewModelBase> items, int timeout)
            {
                return Task.FromResult(true);
            }

            public bool IsStale
            {
                get { return _user.LastRefresh == null || (DateTime.Now - _user.LastRefresh.Value).TotalMinutes > 30; }
            }

            public bool HasMore()
            {
                return _user.OldestActivity != null || !_hasLoaded;
            }

            public async Task<IEnumerable<ViewModelBase>> LoadMore()
            {
                _hasLoaded = true;
                if (_user.OldestActivity != null)
                    await _user.PullOlder();
                else
                    await _user.PullNew();

                return Enumerable.Empty<ViewModelBase>();
            }

            public async Task Refresh(System.Collections.ObjectModel.ObservableCollection<ViewModelBase> current, bool onlyNew)
            {
                _hasLoaded = true;
                await _user.PullNew();
            }

            public string NameForStatus
            {
                get { return _user.Thing.Name + "activitie"; }
            }

            public void Attach(System.Collections.ObjectModel.ObservableCollection<ViewModelBase> targetCollection)
            {
                _activityAggregate = new ActivityGroupViewModel.SelfActivityAggregate(_user.Groups, targetCollection);
            }
        }

        public ObservableSortedUniqueCollection<string, ActivityGroupViewModel> Groups { get; private set; }
		public ObservableCollection<ViewModelBase> Activities { get; private set; }
        public Account Thing { get; set; }
        public string CakeDay
        {
            get
            {
                return Thing.CreatedUTC.ToString("MMMM d");
            }
        }
        public bool Loading { get; set; }
        private string OldestActivity { get; set; }
        public DateTime? LastRefresh { get; set; }
        public AboutUserViewModel(string username)
        {
            Thing = new Account { Name = username };
            Groups = new ObservableSortedUniqueCollection<string, ActivityGroupViewModel>(new ActivityGroupViewModel.ActivityAgeComparitor());
            Activities = SnooStreamViewModel.SystemServices.MakeIncrementalLoadCollection(new UserActivityLoader(this));
            var loadTask = LoadAccount(username);
        }

        public AboutUserViewModel(Account thing, DateTime? lastRefresh)
        {
            Thing = thing;
            Groups = new ObservableSortedUniqueCollection<string, ActivityGroupViewModel>(new ActivityGroupViewModel.ActivityAgeComparitor());
            Activities = SnooStreamViewModel.SystemServices.MakeIncrementalLoadCollection(new UserActivityLoader(this));
        }

        private async Task LoadAccount(string username)
        {
            try
            {
                Loading = true;
                RaisePropertyChanged("Loading");
                Thing = (await SnooStreamViewModel.RedditService.GetUserInfo(username, "about", null)).Data as Account;
                LastRefresh = DateTime.UtcNow;
                RaisePropertyChanged("Thing");
                RaisePropertyChanged("Cakeday");
            }
            finally
            {
                Loading = false;
                RaisePropertyChanged("Loading");
            }
        }

        public Task MaybeRefresh()
        {
            var validLastRefresh = LastRefresh ?? DateTime.UtcNow;
            if ((validLastRefresh - DateTime.UtcNow).TotalMinutes > 15)
                return Refresh(false);
            else
                return Task.FromResult<bool>(true);
        }

        public async Task Refresh(bool onlyNew)
        {
            if (Thing != null && !Loading)
            {
                await LoadAccount(Thing.Name);
                await PullNew();
            }
        }

        private async Task PullNew()
        {
            LastRefresh = DateTime.Now;
            Listing activity = null;
            await SnooStreamViewModel.NotificationService.Report("getting activitiy for " + Thing.Name, async () =>
            {
                activity = await SnooStreamViewModel.RedditService.GetPostsByUser(Thing.Name, null);
            });

            OldestActivity = ActivityGroupViewModel.ProcessListing(Groups, activity, null);

        }

        private async Task PullOlder()
        {
            OldestActivity = await PullActivity(OldestActivity, "activity", string.Format(Reddit.PostByUserBaseFormat, Thing.Name));
        }

        private async Task<string> PullActivity(string oldest, string displayName, string uriFormat)
        {
            Listing activity = null;
            if (!string.IsNullOrWhiteSpace(oldest))
            {
                await SnooStreamViewModel.NotificationService.Report("getting additional " + displayName, async () =>
                {
                    activity = await SnooStreamViewModel.RedditService.GetAdditionalFromListing(uriFormat, oldest, null);
                });

                return ActivityGroupViewModel.ProcessListing(Groups, activity, oldest);
            }
            else
                return oldest;
        }

        public RelayCommand GotoMessage
        {
            get
            {
                return new RelayCommand(() => SnooStreamViewModel.NavigationService.NavigateToMessageReply(new CreateMessageViewModel { Username = Thing.Name }));
            }
        }

        public RelayCommand ToggleFriend
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    if(Thing.IsFriend)
                        await SnooStreamViewModel.RedditService.Unfriend(Thing.Name, "", "friend");
                    else
                        await SnooStreamViewModel.RedditService.Friend(Thing.Name, "", "friend", "");

                    Thing.IsFriend = !Thing.IsFriend;
                    RaisePropertyChanged("Thing");
                });
            }
        }

        public RelayCommand GildUser
        {
            get
            {
                return new RelayCommand(() => SnooStreamViewModel.SystemServices.ShowMessage("Not implemented", "Gilding is not currently implemented please post to /r/Snoostream if this feature is important to you"));
            }
        }
        
    }
}
