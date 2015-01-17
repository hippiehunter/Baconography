using GalaSoft.MvvmLight;
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
        //public class UserActivityLoader : IIncrementalCollectionLoader<ViewModelBase>
        //{
        //    AboutUserViewModel _user;
        //    ActivityGroupViewModel.SelfActivityAggregate _activityAggregate;
        //    public UserActivityLoader(AboutUserViewModel user)
        //    {
        //        _user = user;
        //    }

        //    public Task AuxiliaryItemLoader(IEnumerable<ViewModelBase> items, int timeout)
        //    {
        //        return Task.FromResult(true);
        //    }

        //    public bool IsStale
        //    {
        //        get { return _user.LastRefresh == null || (DateTime.Now - _user.LastRefresh.Value).TotalMinutes > 30; }
        //    }

        //    public bool HasMore()
        //    {
        //        return _user.OldestActivity != null;
        //    }

        //    public async Task<IEnumerable<ViewModelBase>> LoadMore()
        //    {
        //        await _user.PullOlder();
        //        return Enumerable.Empty<ViewModelBase>();
        //    }

        //    public async Task Refresh(System.Collections.ObjectModel.ObservableCollection<ViewModelBase> current, bool onlyNew)
        //    {
        //        await _user.PullNew(!onlyNew);
        //    }

        //    public string NameForStatus
        //    {
        //        get { return _user.Thing.Name + "activitie"; }
        //    }

        //    public void Attach(System.Collections.ObjectModel.ObservableCollection<ViewModelBase> targetCollection)
        //    {
        //        _activityAggregate = new ActivityGroupViewModel.SelfActivityAggregate(_user.Groups, targetCollection);
        //    }
        //}

        public ObservableSortedUniqueCollection<string, ActivityGroupViewModel> Groups { get; private set; }
		public ObservableCollection<ViewModelBase> Activities { get; private set; }
        public static Dictionary<string, ActivityViewModel> ActivityLookup = new Dictionary<string, ActivityViewModel>();
        public Account Thing { get; set; }
        public bool Loading { get; set; }
        private DateTime? LastRefresh { get; set; }
        public AboutUserViewModel(string username, DateTime? lastRefresh)
        {
            var loadTask = LoadAccount(username);
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
            if (Thing != null && !Loading && (validLastRefresh - DateTime.UtcNow).TotalMinutes > 15)
                return LoadAccount(Thing.Name);
            else
                return Task.FromResult<bool>(true);
        }

        public Task Refresh(bool onlyNew)
        {
            if (Thing != null && !Loading)
                return LoadAccount(Thing.Name);
            else
                return Task.FromResult<bool>(true);
        }
    }
}
