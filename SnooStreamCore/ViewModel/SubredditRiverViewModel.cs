using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using SnooSharp;
using SnooStream.Common;
using SnooStream.Messages;
using SnooStream.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class SubredditRiverViewModel : ViewModelBase
    {
        static public string SmallGroupNameSelector(LinkRiverViewModel viewModel)
        {
            return viewModel.IsLocal ? "favorites" : "subscribed";
        }

        static public string LargeGroupNameSelector(LinkRiverViewModel viewModel)
        {
            return viewModel.Thing.DisplayName.Substring(0, 1);
        }
        
        internal ObservableCollection<LinkRiverViewModel> CombinedRivers { get; set; }
		private ObservableCollection<LinkRiverViewModel> _subreddits;
		public ObservableCollection<LinkRiverViewModel> Subreddits { get { return _subreddits; } set { _subreddits = value; RaisePropertyChanged("Subreddits"); } }
        public LinkRiverViewModel SelectedRiver { get; private set; }
		private string _searchString;
		public string SearchString
		{
			get
			{
				return _searchString;
			}
			set
			{
				bool wasChanged = _searchString != value;
				if (wasChanged)
				{
					_searchString = value;

					if (_searchString.Length < 3)
					{
						Subreddits = CombinedRivers;
						RevokeQueryTimer();
					}
					else
					{
						RestartQueryTimer();
					}
				}
			}
		}

		Object _queryTimer;
		void RevokeQueryTimer()
		{
			if (_queryTimer != null)
			{
				SnooStreamViewModel.SystemServices.StopTimer(_queryTimer);
				_queryTimer = null;
			}
		}

		void RestartQueryTimer()
		{
			// Start or reset a pending query
			if (_queryTimer == null)
			{
				_queryTimer = SnooStreamViewModel.SystemServices.StartTimer(queryTimer_Tick, new TimeSpan(0, 0, 1), true);
			}
			else
			{
				SnooStreamViewModel.SystemServices.StopTimer(_queryTimer);
				SnooStreamViewModel.SystemServices.RestartTimer(_queryTimer);
			}
		}

		void queryTimer_Tick(object sender, object timer)
		{
			// Stop the timer so it doesn't fire again unless rescheduled
			RevokeQueryTimer();

			if (!(_searchString != null && _searchString.Contains("/")))
			{
				Subreddits = SnooStreamViewModel.SystemServices.MakeIncrementalLoadCollection(new SubreditSearchLoader(_searchString, CombinedRivers));
			}
		}

		private class SubreditSearchLoader : IIncrementalCollectionLoader<LinkRiverViewModel>
		{
			string _afterSearch = "";
			string _afterUri = "";
			string _searchString;
			bool _hasLoaded = false;
			IEnumerable<LinkRiverViewModel> _existing;

			public SubreditSearchLoader(string searchString, IEnumerable<LinkRiverViewModel> existing)
			{
				_existing = existing;
				_searchString = searchString;
			}

			public Task AuxiliaryItemLoader(IEnumerable<LinkRiverViewModel> items, int timeout)
			{
				return Task.FromResult(true);
			}

			public bool IsStale
			{
				get { return false; }
			}

			public bool HasMore()
			{
				return !_hasLoaded || !string.IsNullOrWhiteSpace(_afterSearch);
			}

			public async Task<IEnumerable<LinkRiverViewModel>> LoadMore()
			{
				var result = new List<LinkRiverViewModel>();
				if (string.IsNullOrWhiteSpace(_afterSearch))
				{
					var searchListing = await SnooStreamViewModel.RedditService.Search(_searchString, null, true, null);
					if (searchListing != null)
					{
						_afterUri = searchListing.Item1;
						_afterSearch = searchListing.Item2.Data.After;
						foreach (var subreddit in searchListing.Item2.Data.Children)
						{
							if (subreddit.Data is Subreddit)
							{
								var existing = _existing.FirstOrDefault(lrvm => lrvm.Thing.Id == ((Subreddit)subreddit.Data).Id);
								if (existing != null)
									result.Add(existing);
								else
								{
									//TODO: this represents an oddity, we loose the cached links by creating a new LinkRiverViewModel
									result.Add(new LinkRiverViewModel(false, subreddit.Data as Subreddit, null, null, null));
								}
							}
						}
					}
				}
				else
				{
					var searchListing = await SnooStreamViewModel.RedditService.GetAdditionalFromListing(_afterUri, _afterSearch);
					_afterSearch = searchListing.Data.After;
					foreach (var subreddit in searchListing.Data.Children)
					{
						if (subreddit.Data is Subreddit)
						{
							var existing = _existing.FirstOrDefault(lrvm => lrvm.Thing.Id == ((Subreddit)subreddit.Data).Id);
							if (existing != null)
								result.Add(existing);
							else
							{
								//TODO: this represents an oddity, we loose the cached links by creating a new LinkRiverViewModel
								result.Add(new LinkRiverViewModel(false, subreddit.Data as Subreddit, null, null, null));
							}
						}
					}
				}
				return result;
			}

			public Task Refresh(ObservableCollection<LinkRiverViewModel> current, bool onlyNew)
			{
				throw new NotImplementedException();
			}
		}

        public SubredditRiverViewModel(SubredditRiverInit initBlob)
        {
            if (initBlob != null)
            {
                var localSubreddits = initBlob.Pinned.Select(blob => new LinkRiverViewModel(true, blob.Thing, blob.DefaultSort, blob.Links, blob.LastRefresh ?? DateTime.Now));
				var subscribbedSubreddits = initBlob.Subscribed.Select(blob => new LinkRiverViewModel(false, blob.Thing, blob.DefaultSort, blob.Links, blob.LastRefresh ?? DateTime.Now));
                
                CombinedRivers = new ObservableCollection<LinkRiverViewModel>(localSubreddits.Concat(subscribbedSubreddits));
                EnsureFrontPage();
                ReloadSubscribed(false);
            }
            else
            {
                LoadWithoutInitial();
                EnsureFrontPage();
            }
			Subreddits = CombinedRivers;
            SelectedRiver = CombinedRivers.FirstOrDefault() ?? new LinkRiverViewModel(true, new Subreddit("/"), "hot", null, null);
            Messenger.Default.Register<UserLoggedInMessage>(this, OnUserLoggedIn);
        }

        public void SelectSubreddit(LinkRiverViewModel viewModel)
        {
            SelectedRiver = viewModel;
            RaisePropertyChanged("SelectSubreddit");
        }

        private void OnUserLoggedIn(UserLoggedInMessage obj)
        {
            ReloadSubscribed(true);
        }

        private void EnsureFrontPage()
        {
            if (!CombinedRivers.Any((lrvm) => lrvm.Thing.Url == "/"))
            {
                CombinedRivers.Add(new LinkRiverViewModel(true, new Subreddit("/"), "hot", null, null));
            }
        }

        private async void LoadWithoutInitial()
        {
            CombinedRivers = new ObservableCollection<LinkRiverViewModel>();
            Listing subscribedListing = null; 
            if (SnooStreamViewModel.RedditUserState != null && !string.IsNullOrWhiteSpace(SnooStreamViewModel.RedditUserState.Username))
            {
                subscribedListing = await SnooStreamViewModel.RedditService.GetSubscribedSubredditListing() ?? await SnooStreamViewModel.RedditService.GetDefaultSubreddits();
            }
            else
            {
                subscribedListing = await SnooStreamViewModel.RedditService.GetDefaultSubreddits();
                subscribedListing.Data.Children.Insert(0, new Thing { Data = new Subreddit { Name = "gifs", DisplayName = "gifs", Url = "/r/gifs", Title = "gifs" } });
            }

            foreach (var river in subscribedListing.Data.Children.Select(thing => new LinkRiverViewModel(false, thing.Data as Subreddit, "hot", null, null)))
            {
                CombinedRivers.Add(river);
            }
        }

        private async void ReloadSubscribed(bool required)
        {
            if (!required)
            {
				if (SnooStreamViewModel.SystemServices == null)
					await Task.Delay(1000);
				//dump out something is very wrong
				if (SnooStreamViewModel.SystemServices == null)
					return;

                if (!SnooStreamViewModel.SystemServices.IsLowPriorityNetworkOk)
                    return;
                else
                    await Task.Delay(10000);
            }

            var subscribedListing = await SnooStreamViewModel.RedditService.GetSubscribedSubredditListing();

			var newRivers = new Dictionary<string, Subreddit>();
			foreach (var subreddit in subscribedListing.Data.Children)
			{
				if(subreddit.Data is Subreddit && !newRivers.ContainsKey(((Subreddit)subreddit.Data).Id))
					newRivers.Add(((Subreddit)subreddit.Data).Id, ((Subreddit)subreddit.Data));
			}

			var missingRivers = new List<LinkRiverViewModel>();
			var existingRivers = new Dictionary<string, LinkRiverViewModel>();
			foreach (var river in CombinedRivers)
			{
				//ignore the frontpage
				if (string.IsNullOrWhiteSpace(river.Thing.Id))
					continue;

				if (!existingRivers.ContainsKey(river.Thing.Id))
					existingRivers.Add(river.Thing.Id, river);

				if(!river.IsLocal && !newRivers.ContainsKey(river.Thing.Id))
					missingRivers.Add(river);
				else if(newRivers.ContainsKey(river.Thing.Id))
					river.Thing = newRivers[river.Thing.Id];
			}

			
			foreach (var subredditTpl in newRivers)
			{
				if(!existingRivers.ContainsKey(subredditTpl.Key))
					CombinedRivers.Add(new LinkRiverViewModel(false, subredditTpl.Value,  "hot", null, null));
			}

			foreach (var missingRiver in missingRivers)
			{
				CombinedRivers.Remove(missingRiver);
			}
        }

		internal SubredditRiverInit Dump()
		{
			return new SubredditRiverInit
			{
				Pinned = CombinedRivers
					.Where(vm => vm.IsLocal)
					.Select(vm => vm.Dump())
					.ToList(),
				Subscribed = CombinedRivers
					.Where(vm => !vm.IsLocal)
					.Select(vm => vm.Dump())
					.ToList(),
			};
		}
	}
}
