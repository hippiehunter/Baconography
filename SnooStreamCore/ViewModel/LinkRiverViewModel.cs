using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SnooSharp;
using SnooStream.Common;
using SnooStream.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class LinkRiverViewModel : ViewModelBase, IRefreshable, IHasLinks
    {
        //need to come up with an init blob setup for this, meaining a per river blob
        public Subreddit Thing { get; internal set; }
		public int HeaderImageWidth { get { return GetHeaderSizeOrDefault(true); } }
		public int HeaderImageHeight { get { return GetHeaderSizeOrDefault(false); } }
        public string Sort { get; private set; }
        private string LastLinkId { get; set; }
		public DateTime? LastRefresh { get; set; }
        public bool IsLocal { get; private set; }
        public bool IsUserMultiReddit
        {
            get
            {
                if (Thing == null || Thing.Url == "/")
                    return false;
                else
                    return Thing.Url.Contains("/m/") || Thing.Url.Contains("+");
            }
        }

        public bool IsMultiReddit
        {
            get
            {
                if (Thing == null || Thing.Url == "/")
                    return true;
                else
                    return Thing.Url.Contains("/m/") || Thing.Url.Contains("+");
            }
        }

        public string MultiRedditUser
        {
            get
            {
                if (IsMultiReddit && Thing.Url.Length > 2)
                {
                    if (Thing.Url.Contains("/me/"))
                    {
                        return SnooStreamViewModel.RedditUserState.Username;
                    }
                    int endOfSlashU = Thing.Url.IndexOf("/", 2);
                    int startOfSlashM = Thing.Url.IndexOf("/m/", endOfSlashU);
                    return Thing.Url.Substring(endOfSlashU + 1, startOfSlashM - endOfSlashU - 1);
                }
                else
                    return "";
            }
        }


		private const int DefaultHeaderWidth = 125;
		private const int DefaultHeaderHeight = 50;

		private int GetHeaderSizeOrDefault (bool width)
		{
			if(Thing.HeaderSize == null || Thing.HeaderSize.Length < 2)
				return width ? DefaultHeaderWidth : DefaultHeaderHeight;
			else
				return width ? Thing.HeaderSize[0] : Thing.HeaderSize[1];
		}

		public LinkRiverViewModel(bool isLocal, Subreddit thing, string sort, IEnumerable<Link> initialLinks, DateTime? lastRefreshed)
		{
			LastRefresh = lastRefreshed;
			IsLocal = isLocal;
			Thing = thing;
			Sort = sort ?? "hot";
			Links = SnooStreamViewModel.SystemServices.MakeIncrementalLoadCollection(new LinksLoader(this));
			if (initialLinks != null)
			{
				ProcessLinkThings(initialLinks);
			}
		}

		private class LinksLoader : IIncrementalCollectionLoader<ILinkViewModel>
		{
			LinkRiverViewModel _linkRiverViewModel;
			public LinksLoader(LinkRiverViewModel linkRiverViewModel)
			{
				_linkRiverViewModel = linkRiverViewModel;
			}

			public void Attach(ObservableCollection<ILinkViewModel> targetCollection) { }

			public Task AuxiliaryItemLoader(IEnumerable<ILinkViewModel> items, int timeout)
			{
				return Task.FromResult(true);
			}

			public bool IsStale
			{
				get { return _linkRiverViewModel.LastRefresh == null || (DateTime.Now - _linkRiverViewModel.LastRefresh.Value).TotalMinutes > 30; }
			}

			public bool HasMore()
			{
				return _linkRiverViewModel.Links.Count == 0 || _linkRiverViewModel.LastLinkId != null;
			}

			public async Task<IEnumerable<ILinkViewModel>> LoadMore()
			{
				var result = new List<LinkViewModel>();
				var postListing = _linkRiverViewModel.LastLinkId != null ?
						await SnooStreamViewModel.RedditService.GetAdditionalFromListing(_linkRiverViewModel.Thing.Url + ".json?sort=" + _linkRiverViewModel.Sort, _linkRiverViewModel.LastLinkId) :
						await SnooStreamViewModel.RedditService.GetPostsBySubreddit(_linkRiverViewModel.Thing.Url, _linkRiverViewModel.Sort);

				if (postListing != null)
				{
					_linkRiverViewModel.LastRefresh = DateTime.Now;
					var linkIds = new List<string>();
					foreach (var thing in postListing.Data.Children)
					{
						if (thing.Data is Link)
						{
							linkIds.Add(((Link)thing.Data).Id);
							var viewModel = new LinkViewModel(_linkRiverViewModel, thing.Data as Link) { FromMultiReddit = (_linkRiverViewModel.IsMultiReddit || _linkRiverViewModel.Thing.Url == "/") };
							result.Add(viewModel);
						}
					}
					var linkMetadata = (await SnooStreamViewModel.OfflineService.GetLinkMetadata(linkIds)).ToList();
					for (int i = 0; i < linkMetadata.Count; i++)
					{
						result[i].UpdateMetadata(linkMetadata[i]);
					}
					_linkRiverViewModel.LastLinkId = postListing.Data.After;
				}
				return result;
			}

			public async Task Refresh(ObservableCollection<ILinkViewModel> current, bool onlyNew)
			{
				var postListing = await SnooStreamViewModel.RedditService.GetPostsBySubreddit(_linkRiverViewModel.Thing.Url, _linkRiverViewModel.Sort); 
				if (postListing != null)
				{
					_linkRiverViewModel.LastRefresh = DateTime.Now;
					var linkIds = new List<string>();
					var replace = new List<Tuple<int, ILinkViewModel>>();
					var move = new List<Tuple<int, int, ILinkViewModel>>();
					var existing = new Dictionary<string, Tuple<int, ILinkViewModel>>();
					var update = new List<ILinkViewModel>();
					for (int i = 0; i < postListing.Data.Children.Count; i++)
					{
						var thing = postListing.Data.Children[i];
						if (thing.Data is Link)
						{
							linkIds.Add(((Link)thing.Data).Id);
							var viewModel = new LinkViewModel(_linkRiverViewModel, thing.Data as Link);
							replace.Add(Tuple.Create<int, ILinkViewModel>(i, viewModel));
						}
					}

					for (int i = 0; i < current.Count; i++)
					{
						if (!existing.ContainsKey(current[i].Id))
							existing.Add(current[i].Id, Tuple.Create(i, current[i]));
					}

					foreach (var link in replace)
					{
						if (existing.ContainsKey(link.Item2.Id))
						{
							var existingIndex = existing[link.Item2.Id].Item1;
							if (existingIndex == link.Item1)
								update.Add(link.Item2);
							else
								move.Add(Tuple.Create(existingIndex, link.Item1, link.Item2));
						}
					}
					replace.RemoveAll((tpl) => update.Contains(tpl.Item2) || move.Any(tpl2 => tpl2.Item3 == tpl.Item2));

					SnooStreamViewModel.SystemServices.RunUIIdleAsync(async () =>
					{
						foreach (var link in update)
						{
							((LinkViewModel)existing[link.Id].Item2).MergeLink(((LinkViewModel)link).Link);
						}

						foreach (var linkTpl in move)
						{
							((LinkViewModel)existing[linkTpl.Item3.Id].Item2).MergeLink(((LinkViewModel)linkTpl.Item3).Link);
						}

						bool unfinished = true;
						while (unfinished)
						{
							unfinished = false;
							foreach (var linkTpl in move.OrderBy(tpl => tpl.Item2))
							{
								var currentIndex = current.IndexOf(linkTpl.Item3);
								if (currentIndex > 0 && currentIndex != linkTpl.Item2)
								{
									unfinished = true;
									current.Move(currentIndex, linkTpl.Item2);
								}
							}
						}

						foreach (var newLink in replace.OrderBy(tpl => tpl.Item1))
						{
							if (current.Count - 1 > newLink.Item1)
								current[newLink.Item1] = newLink.Item2;
							else
								current.Add(newLink.Item2);
						}

						for (int i = current.Count - 1; i > linkIds.Count; i--)
						{
							current.RemoveAt(i);
						}

						var linkMetadata = (await SnooStreamViewModel.OfflineService.GetLinkMetadata(linkIds)).ToList();
						for (int i = 0; i < linkMetadata.Count; i++)
						{
							((LinkViewModel)current[i]).UpdateMetadata(linkMetadata[i]);
						}
						_linkRiverViewModel.LastLinkId = postListing.Data.After;
					});
				}
				if(_linkRiverViewModel != null)
					_linkRiverViewModel.CurrentSelected = _linkRiverViewModel.Links.FirstOrDefault();
			}


			public string NameForStatus
			{
				get { return "post"; }
			}
		}

        private void ProcessLinkThings(IEnumerable<Link> links)
        {
            foreach (var link in links)
            {
				Links.Add(new LinkViewModel(this, link) { FromMultiReddit = (IsMultiReddit || Thing.Url == "/") });
            }
        }

		public ObservableCollection<ILinkViewModel> Links { get; set; }

		ILinkViewModel _currentSelected;
		public ILinkViewModel CurrentSelected
		{
			get
			{
				if (IsInDesignMode)
					return new LinkViewModel(this, new Link { Title = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras ac tempor erat. Cras sagittis eu urna sed posuere. Proin sit amet fringilla magna. Sed feugiat lorem nibh, ac mollis risus rutrum non. Pellentesque pharetra auctor pellentesque. Maecenas vel lorem sagittis.", Domain = "http://www.google.com", Author = "fredbob", Url = "http://www.google.com", CommentCount = 2453 });
				else
					return _currentSelected;
			}
			set
			{
				_currentSelected = value;
				RaisePropertyChanged("CurrentSelected");
			}
		}

		internal SubredditInit Dump()
		{
			return new SubredditInit
			{
				DefaultSort = Sort,
				Thing = Thing,
				Links = Links.Select(vm => ((LinkViewModel)vm).Link).ToList(),
				LastRefresh = LastRefresh
			};
		}

		public async Task MaybeRefresh()
		{
			await ((IRefreshable)Links).MaybeRefresh();
		}

		public async Task Refresh(bool onlyNew)
		{
			await ((IRefreshable)Links).Refresh(onlyNew);
		}

		public async void SetSort(string sort)
		{
			Sort = sort;
			await Refresh(false);
		}

		public RelayCommand RefreshCommand
		{
			get
			{
				return new RelayCommand(() => Refresh(false));
			}
		}

		public RelayCommand CreateNewPostCommand
		{
			get
			{
				return new RelayCommand(() => SnooStreamViewModel.NavigationService.NavigateToPost(new PostViewModel()));
			}
		}

		public RelayCommand FindInSubredditCommand
		{
			get
			{
				return new RelayCommand(() => SnooStreamViewModel.NavigationService.NavigateToSearch(new SearchViewModel()));
			}
		}
	}
}
