using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using MetroLog;
using SnooSharp;
using SnooStream.Common;
using SnooStream.Messages;
using SnooStream.Services;
using SnooStream.ViewModel.Popups;
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
            public ObservableCollection<SubredditWrapper> Collection { get; set; }
        }

        public class SubredditWrapper : ViewModelBase
        {
            public SubredditWrapper(SubredditRiverViewModel context, string name, Subreddit thing, string sort, string category)
            {
                Thing = thing;
                Name = name;
                _category = category;
                _context = context;
                _linkRiver = new Lazy<LinkRiverViewModel>(() =>
                {
                    if (_context._madeSubreddits.ContainsKey(name))
                        return _context._madeSubreddits[name];
                    else
                    {
                        var result = new LinkRiverViewModel(_context, category, thing, sort, null, null);
                        _context._madeSubreddits.Add(name, result);
                        return result;
                    }
                });
            }

            private SubredditRiverViewModel _context;
            public string Name { get; set; }
            public Subreddit Thing { get; set; }
            private string _category;
            public string Category
            {
                get
                {
                    return _category;
                }
                set
                {
                    if (_category != value)
                    {
                        _category = value;
                        RaisePropertyChanged("Category");

                        if (_linkRiver.IsValueCreated)
                        {
                            _linkRiver.Value.Category = value;
                        }

                        if (_context != null)
                        {
                            _context.RemoveFromCategory(_context.SubredditCollection, this);
                            _context.AddToCategory(_context.SubredditCollection, this);
                        }
                    }

                }
            }
            public int HeaderImageWidth { get { return GetHeaderSizeOrDefault(true); } }
            public int HeaderImageHeight { get { return GetHeaderSizeOrDefault(false); } }
        
            private Lazy<LinkRiverViewModel> _linkRiver;
            public LinkRiverViewModel LinkRiver
            {
                get
                {
                    return _linkRiver.Value;
                }
            }

            private const int DefaultHeaderWidth = 125;
            private const int DefaultHeaderHeight = 50;

            private int GetHeaderSizeOrDefault(bool width)
            {
                if (Thing.HeaderSize == null || Thing.HeaderSize.Length < 2)
                    return width ? DefaultHeaderWidth : DefaultHeaderHeight;
                else
                    return width ? Thing.HeaderSize[0] : Thing.HeaderSize[1];
            }

            void ShowCategoryPicker(object elementTarget)
            {
                SnooStreamViewModel.NavigationService.ShowPopup(new InputViewModel { Prompt = "Choose a category to group this subreddit", InputValue = Category, Dismissed = new RelayCommand<string>(val => Category = val) }, elementTarget, SnooStreamViewModel.UIContextCancellationToken);
            }

            public List<CommandViewModel.CommandItem> MakeSubredditManagmentCommands(object elementTarget)
            {
                var result = new List<CommandViewModel.CommandItem>();

                if (LinkRiver.IsUserMultiReddit)
                {
                    //About
                    result.Add(new CommandViewModel.CommandItem
                    {
                        DisplayText = "About",
                        Command = LinkRiver.ShowAboutSubreddit
                    });
                    //Modify
                    result.Add(new CommandViewModel.CommandItem
                    {
                        DisplayText = "Modify",
                        Command = LinkRiver.ShowMultiRedditManagement
                    });
                    //Delete
                    result.Add(new CommandViewModel.CommandItem
                    {
                        DisplayText = "Delete",
                        Command = LinkRiver.DeleteMultiReddit
                    });
                }
                else
                {
                    //About
                    if (!LinkRiver.IsMultiReddit)
                    {
                        result.Add(new CommandViewModel.CommandItem
                        {
                            DisplayText = "About",
                            Command = LinkRiver.ShowAboutSubreddit
                        });
                    }


                    if (!Thing.Subscribed)
                    {
                        //Subscribe
                        result.Add(new CommandViewModel.CommandItem
                        {
                            DisplayText = "Subscribe",
                            Command = LinkRiver.Subscribe
                        });

                        if (LinkRiver.Context != null && !LinkRiver.Context.LocalSubreddits.Contains(this))
                        {
                            result.Add(new CommandViewModel.CommandItem
                            {
                                DisplayText = "Pin",
                                Command = LinkRiver.Pin
                            });
                        }
                    }
                    else
                    {
                        //Unsubscribe
                        result.Add(new CommandViewModel.CommandItem
                        {
                            DisplayText = "Unsubscribe",
                            Command = LinkRiver.Unsubscribe
                        });
                    }
                }

                //Change Category
                result.Add(new CommandViewModel.CommandItem
                {
                    DisplayText = "Change Category",
                    Command = new RelayCommand(() => ShowCategoryPicker(elementTarget))
                });

                return result;

            }
        }
		static ILogger _logger = LogManagerFactory.DefaultLogManager.GetLogger<SnooStreamViewModel>();
        Dictionary<string, LinkRiverViewModel> _madeSubreddits = new Dictionary<string, LinkRiverViewModel>();
        ObservableCollection<SubredditGroup> _subredditCollection;
        public ObservableCollection<SubredditGroup> SubredditCollection
        {
            get
            {
                return _subredditCollection;
            }
        }

        internal LinkRiverViewModel GetOrMakeSubreddit(string category, Subreddit thing, string sort, IEnumerable<Link> initialLinks, DateTime? lastRefreshed)
        {
            if (_madeSubreddits.ContainsKey(thing.Url))
                return _madeSubreddits[thing.Url];
            else
            {
                var result = new LinkRiverViewModel(this, category, thing, sort, initialLinks, lastRefreshed);
                _madeSubreddits.Add(thing.Url, result);
                return result;
            }
        }

        ObservableCollection<SubredditWrapper> LocalSubreddits { get; set; }
        ObservableCollection<SubredditWrapper> SearchSubreddits { get; set; }

        private void AttachCollection(ObservableCollection<SubredditWrapper> sourceCollection)
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
                    AddToCategory(_subredditCollection, e.NewItems[0] as SubredditWrapper);
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveFromCategory(_subredditCollection, e.OldItems[0] as SubredditWrapper);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    RemoveFromCategory(_subredditCollection, e.OldItems[0] as SubredditWrapper);
                    AddToCategory(_subredditCollection, e.NewItems[0] as SubredditWrapper);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    _subredditCollection.Clear();
                    break;
                default:
                    break;
            }
        }

        private void AddToCategory(ObservableCollection<SubredditGroup> target, SubredditWrapper viewModel)
        {
            var category = viewModel.Category;
            var existingCategory = target.FirstOrDefault((group) => string.Compare(group.Name, category, StringComparison.CurrentCultureIgnoreCase) == 0);
            if (existingCategory != null)
            {
                existingCategory.Collection.Add(viewModel);
            }
            else
            {
                target.Add(new SubredditGroup { Name = category, Collection = new ObservableCollection<SubredditWrapper> { viewModel } });
                IsShowingGroups = target.Count > 1;
            }
        }

        private void RemoveFromCategory(ObservableCollection<SubredditGroup> target, SubredditWrapper viewModel)
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


        private void DetachCollection(ObservableCollection<SubredditWrapper> targetCollection)
        {
            if (targetCollection != null)
            {
                SnooStreamViewModel.SystemServices.FilterDetachIncrementalLoadCollection(_subredditCollection, targetCollection);
                targetCollection.CollectionChanged -= sourceCollection_CollectionChanged;
                IsShowingGroups = false;
                _subredditCollection.Clear();
            }
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
                        DetachCollection(SearchSubreddits);
                        SearchSubreddits = null;
                        AttachCollection(LocalSubreddits);
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
				SearchSubreddits = SnooStreamViewModel.SystemServices.MakeIncrementalLoadCollection(new SubreditSearchLoader(_searchString, this));
                DetachCollection(LocalSubreddits);
                AttachCollection(SearchSubreddits);
            }
		}

		private class SubreditSearchLoader : IIncrementalCollectionLoader<SubredditWrapper>
		{
			string _afterSearch = "";
			string _afterUri = "";
			string _searchString;
			bool _hasLoaded = false;
            SubredditRiverViewModel _context;

            public SubreditSearchLoader(string searchString, SubredditRiverViewModel context)
			{
                _context = context;
				_searchString = searchString;
			}

            public void Attach(ObservableCollection<SubredditWrapper> targetCollection) { }

            public Task AuxiliaryItemLoader(IEnumerable<SubredditWrapper> items, int timeout)
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

            public async Task<IEnumerable<SubredditWrapper>> LoadMore()
			{
				_hasLoaded = true;
                var result = new List<SubredditWrapper>();
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

            private void MakeSubreddit(List<SubredditWrapper> result, Thing subreddit, string category)
            {
                if (subreddit.Data is Subreddit)
                {
                    result.Add(new SubredditWrapper(_context, ((Subreddit)subreddit.Data).Url, subreddit.Data as Subreddit, "hot", category));
                }
            }

            public Task Refresh(ObservableCollection<SubredditWrapper> current, bool onlyNew)
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
            if (initBlob != null && initBlob.Local != null)
            {
                LocalSubreddits = new ObservableCollection<SubredditWrapper>(initBlob.Local.Select(blob => new SubredditWrapper(this, blob.Thing.Url, blob.Thing, blob.DefaultSort, blob.Category ?? "pinned")));
                EnsureFrontPage();
                ReloadSubscribed(false);
            }
            else
            {
                LoadWithoutInitial();
                EnsureFrontPage();
            }

            SelectedRiver = (LocalSubreddits.FirstOrDefault() ?? new SubredditWrapper(this, "/", new Subreddit("/"), "hot", IsLoggedIn ? "subscribed" : "popular")).LinkRiver;
            AttachCollection(LocalSubreddits);
            Messenger.Default.Register<UserLoggedInMessage>(this, OnUserLoggedIn);
        }

        public void SelectSubreddit(LinkRiverViewModel viewModel)
        {
            SelectedRiver = viewModel;
            RaisePropertyChanged("SelectSubreddit");
        }

        private void OnUserLoggedIn(UserLoggedInMessage obj)
        {
            LocalSubreddits.Clear();
            _madeSubreddits.Clear();
            EnsureFrontPage();
            ReloadSubscribed(true);
        }

        private void EnsureFrontPage()
        {
            if (!LocalSubreddits.Any((lrvm) => lrvm.Thing.Url == "/"))
            {
                LocalSubreddits.Add(new SubredditWrapper(this, "/", new Subreddit("/"), "hot", IsLoggedIn ? "subscribed" : "popular"));
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
            LocalSubreddits = new ObservableCollection<SubredditWrapper>();
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

            foreach (var river in subscribedListing.Data.Children.Select(thing => new SubredditWrapper(this, ((Subreddit)thing.Data).Url, thing.Data as Subreddit, "hot", categoryName)))
            {
                LocalSubreddits.Add(river);
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

                var missingRivers = new List<SubredditWrapper>();
                var existingRivers = new Dictionary<string, SubredditWrapper>();
                foreach (var river in LocalSubreddits)
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
                        LocalSubreddits.Add(new SubredditWrapper(this, subredditTpl.Value.Url, subredditTpl.Value, "hot", "subscribed"));
				}

				foreach (var missingRiver in missingRivers)
				{
                    LocalSubreddits.Remove(missingRiver);
				}
			}
			catch (Exception ex)
			{
				_logger.Error("failed refreshing subscribed subreddits", ex);
			}
        }

        public void PinSubreddit(LinkRiverViewModel linkRiver)
        {
            if (!LocalSubreddits.Any((wrap) => wrap.Thing.Url == linkRiver.Thing.Url))
                LocalSubreddits.Add(new SubredditWrapper(this, linkRiver.Thing.Url, linkRiver.Thing, linkRiver.Sort, linkRiver.Category));
        }

		internal SubredditRiverInit Dump()
		{
			return new SubredditRiverInit
			{
                Local = LocalSubreddits
					.Select(vm => vm.LinkRiver.Dump())
					.ToList()
			};
		}
	}
}
