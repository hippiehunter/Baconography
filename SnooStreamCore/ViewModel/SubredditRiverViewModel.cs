using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using MetroLog;
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
        public class SubredditGroup
        {
            public string Name { get; set; }
            public ObservableCollection<LinkRiverViewModel> Collection { get; set; }
        }
		static ILogger _logger = LogManagerFactory.DefaultLogManager.GetLogger<SnooStreamViewModel>();
        internal ObservableCollection<LinkRiverViewModel> CombinedRivers { get; set; }
		private ObservableCollection<LinkRiverViewModel> _subreddits;

        ObservableCollection<SubredditGroup> _subredditCollection;
        public ObservableCollection<SubredditGroup> SubredditCollection
        {
            get
            {
                return _subredditCollection;
            }
        }
		public ObservableCollection<LinkRiverViewModel> Subreddits
        { 
            get 
            { 
                return _subreddits; 
            } 
            set 
            {
                if (_subreddits != value)
                {
                    _subreddits = value;
                    if (_subreddits != null && _subredditCollection != null)
                        DetachCollection(_subreddits);

                    AttachCollection(value);
                    RaisePropertyChanged("Subreddits");
                }
            } 
        }

        private void AttachCollection(ObservableCollection<LinkRiverViewModel> sourceCollection)
        {
            _subredditCollection = SnooStreamViewModel.SystemServices.FilterAttachIncrementalLoadCollection(sourceCollection, _subredditCollection);
            sourceCollection.CollectionChanged += sourceCollection_CollectionChanged;
            foreach (var item in sourceCollection)
            {
                AddToCategory(_subredditCollection, item);
            }

            RaisePropertyChanged("SubredditCollection"); 
        }

        private void sourceCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddToCategory(_subredditCollection, e.NewItems[0] as LinkRiverViewModel);
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveFromCategory(_subredditCollection, e.OldItems[0] as LinkRiverViewModel);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    RemoveFromCategory(_subredditCollection, e.OldItems[0] as LinkRiverViewModel);
                    AddToCategory(_subredditCollection, e.NewItems[0] as LinkRiverViewModel);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    _subredditCollection.Clear();
                    break;
                default:
                    break;
            }
        }

        private void AddToCategory(ObservableCollection<SubredditGroup> target, LinkRiverViewModel viewModel)
        {
            var category = viewModel.Category;
            var existingCategory = target.FirstOrDefault((group) => string.Compare(group.Name, category, StringComparison.CurrentCultureIgnoreCase) == 0);
            if (existingCategory != null)
            {
                existingCategory.Collection.Add(viewModel);
            }
            else
            {
                target.Add(new SubredditGroup { Name = category, Collection = new ObservableCollection<LinkRiverViewModel> { viewModel } });
                IsShowingGroups = target.Count > 1;
            }
        }

        private void RemoveFromCategory(ObservableCollection<SubredditGroup> target, LinkRiverViewModel viewModel)
        {
            var category = viewModel.Category;
            var existingCategory = target.FirstOrDefault((group) => string.Compare(group.Name, category, StringComparison.CurrentCultureIgnoreCase) == 0);
            if (existingCategory != null)
            {
                existingCategory.Collection.Remove(viewModel);
                if (existingCategory.Collection.Count == 0)
                {
                    target.Remove(existingCategory);
                    IsShowingGroups = target.Count > 1;
                }
            }
        }


        private void DetachCollection(ObservableCollection<LinkRiverViewModel> targetCollection)
        {
            SnooStreamViewModel.SystemServices.FilterDetachIncrementalLoadCollection(_subredditCollection, targetCollection);
            targetCollection.CollectionChanged -= sourceCollection_CollectionChanged;
            IsShowingGroups = false;
            _subredditCollection.Clear();
        }

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
				Subreddits = SnooStreamViewModel.SystemServices.MakeIncrementalLoadCollection(new SubreditSearchLoader(_searchString, CombinedRivers, this));
			}
		}

		private class SubreditSearchLoader : IIncrementalCollectionLoader<LinkRiverViewModel>
		{
			string _afterSearch = "";
			string _afterUri = "";
			string _searchString;
			bool _hasLoaded = false;
			IEnumerable<LinkRiverViewModel> _existing;
            SubredditRiverViewModel _context;

            public SubreditSearchLoader(string searchString, IEnumerable<LinkRiverViewModel> existing, SubredditRiverViewModel context)
			{
                _context = context;
                _existing = existing;
				_searchString = searchString;
			}

			public void Attach(ObservableCollection<LinkRiverViewModel> targetCollection) { }

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
				_hasLoaded = true;
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
                            MakeSubreddit(result, subreddit, "");
                        }
					}
				}
				else
				{
					var searchListing = await SnooStreamViewModel.RedditService.GetAdditionalFromListing(_afterUri, _afterSearch);
					_afterSearch = searchListing.Data.After;
					foreach (var subreddit in searchListing.Data.Children)
                    {
                        MakeSubreddit(result, subreddit, "");
                    }
                }
				return result;
			}

            private void MakeSubreddit(List<LinkRiverViewModel> result, Thing subreddit, string category)
            {
                if (subreddit.Data is Subreddit)
                {
                    var existing = _existing.FirstOrDefault(lrvm => lrvm.Thing.Id == ((Subreddit)subreddit.Data).Id);
                    if (existing != null)
                        result.Add(existing);
                    else
                    {
                        //TODO: this represents an oddity, we loose the cached links by creating a new LinkRiverViewModel
                        result.Add(new LinkRiverViewModel(_context, category, subreddit.Data as Subreddit, null, null, null));
                    }
                }
            }

            public Task Refresh(ObservableCollection<LinkRiverViewModel> current, bool onlyNew)
			{
				throw new NotImplementedException();
			}


			public string NameForStatus
			{
				get { return "subreddit search result"; }
			}
		}

        private bool _isShowingGroups = false;
        public bool IsShowingGroups
        {
            get
            {
                return _isShowingGroups;
            }
            set
            {
                if(_isShowingGroups != value)
                {
                    _isShowingGroups = value;
                    RaisePropertyChanged("IsShowingGroups");
                }
            }
        }

        public SubredditRiverViewModel(SubredditRiverInit initBlob)
        {
            if (initBlob != null)
            {
                var localSubreddits = initBlob.Pinned.Select(blob => new LinkRiverViewModel(this, blob.Category ?? "pinned", blob.Thing, blob.DefaultSort, blob.Links, blob.LastRefresh ?? DateTime.Now));
                var subscribbedSubreddits = initBlob.Subscribed.Select(blob => new LinkRiverViewModel(this, blob.Category ?? "subscribed", blob.Thing, blob.DefaultSort, blob.Links, blob.LastRefresh ?? DateTime.Now));
                
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
            SelectedRiver = CombinedRivers.FirstOrDefault() ?? new LinkRiverViewModel(this, IsLoggedIn ? "subscribed" : "popular", new Subreddit("/"), "hot", null, null);
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
                CombinedRivers.Add(new LinkRiverViewModel(this, IsLoggedIn ? "subscribed" : "popular", new Subreddit("/"), "hot", null, null));
            }
        }

        private bool IsLoggedIn
        {
            get
            {
                return SnooStreamViewModel.RedditUserState != null && !string.IsNullOrWhiteSpace(SnooStreamViewModel.RedditUserState.Username);
            }
        }

        private async void LoadWithoutInitial()
        {
            CombinedRivers = new ObservableCollection<LinkRiverViewModel>();
            Listing subscribedListing = null;
            string categoryName = "subscribed";
            if (IsLoggedIn)
            {
                subscribedListing = await SnooStreamViewModel.RedditService.GetSubscribedSubredditListing();
            }
            else
            {
                categoryName = "popular";
                subscribedListing = await SnooStreamViewModel.RedditService.GetSubreddits(25, "popular");
            }

            foreach (var river in subscribedListing.Data.Children.Select(thing => new LinkRiverViewModel(this, categoryName, thing.Data as Subreddit, "hot", null, null)))
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
			try
			{
				var subscribedListing = await SnooStreamViewModel.RedditService.GetSubscribedSubredditListing();

				var newRivers = new Dictionary<string, Subreddit>();
				foreach (var subreddit in subscribedListing.Data.Children)
				{
					if (subreddit.Data is Subreddit && !newRivers.ContainsKey(((Subreddit)subreddit.Data).Id))
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

					if (river.Category != "subscribed" && !newRivers.ContainsKey(river.Thing.Id))
						missingRivers.Add(river);
					else if (newRivers.ContainsKey(river.Thing.Id))
						river.Thing = newRivers[river.Thing.Id];
				}


				foreach (var subredditTpl in newRivers)
				{
					if (!existingRivers.ContainsKey(subredditTpl.Key))
						CombinedRivers.Add(new LinkRiverViewModel(this, "subscribed", subredditTpl.Value, "hot", null, null));
				}

				foreach (var missingRiver in missingRivers)
				{
					CombinedRivers.Remove(missingRiver);
				}
			}
			catch (Exception ex)
			{
				_logger.Error("failed refreshing subscribed subreddits", ex);
			}
        }

        public void PinSubreddit(LinkRiverViewModel linkRiver)
        {
            if (!CombinedRivers.Contains(linkRiver))
                CombinedRivers.Add(linkRiver);
        }

		internal SubredditRiverInit Dump()
		{
			return new SubredditRiverInit
			{
				Pinned = CombinedRivers
					.Where(vm => vm.Category != "subscribed")
					.Select(vm => vm.Dump())
					.ToList(),
				Subscribed = CombinedRivers
                    .Where(vm => vm.Category == "subscribed")
					.Select(vm => vm.Dump())
					.ToList(),
			};
		}
	}
}
