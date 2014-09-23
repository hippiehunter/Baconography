using GalaSoft.MvvmLight;
using SnooSharp;
using SnooStream.Common;
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
    public class LinkRiverViewModel : ViewModelBase, IRefreshable
    {
        //need to come up with an init blob setup for this, meaining a per river blob
        public Subreddit Thing { get; internal set; }
		public int HeaderImageWidth { get { return GetHeaderSizeOrDefault(true); } }
		public int HeaderImageHeight { get { return GetHeaderSizeOrDefault(false); } }
        public string Sort { get; private set; }
        public bool Loading { get { return _loadingTask != null; } }
        private string LastLinkId { get; set; }
		private DateTime? LastRefresh { get; set; }
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
			Links = new PortableObservableCollection<LinkViewModel>(LoadMore);
			if (initialLinks != null)
			{
				ProcessLinkThings(initialLinks);
			}

			if (IsInDesignMode)
			{
				CurrentSelected = new LinkViewModel(this, new Link { Title = "Lorem Ipsum", Domain = "http://www.google.com", Author = "fredbob", Url = "http://www.google.com" });
			}

		}

        private void ProcessLinkThings(IEnumerable<Link> links)
        {
            foreach (var link in links)
            {
                Links.Add(new LinkViewModel(this, link));
            }
        }

        public PortableObservableCollection<LinkViewModel> Links { get; set; }
        private Task _loadingTask;
        public Task LoadMore()
        {
            if (_loadingTask == null)
            {
                lock (this)
                {
                    if (_loadingTask == null)
                    {
                        _loadingTask = LoadMoreImpl();
                    }
                }
            }
            return _loadingTask;
        }

		public Task RefreshTask()
		{
			if (_loadingTask == null)
			{
				lock (this)
				{
					if (_loadingTask == null)
					{
						_loadingTask = RefreshImpl();
					}
				}
			}
			return _loadingTask;
		}

        public async Task LoadMoreImpl()
        {
            await SnooStreamViewModel.NotificationService.Report("loading posts", async () =>
                {
                    var postListing = LastLinkId != null ? 
                        await SnooStreamViewModel.RedditService.GetAdditionalFromListing(Thing.Url + ".json?sort=" + Sort, LastLinkId) :
						await SnooStreamViewModel.RedditService.GetPostsBySubreddit(Thing.Url, Sort);

                    if (postListing != null)
                    {
						LastRefresh = DateTime.Now;
						SnooStreamViewModel.SystemServices.RunUIAsync(async () =>
							{
								var linkIds = new List<string>();
								var linkViewModels = new List<LinkViewModel>();
								foreach (var thing in postListing.Data.Children)
								{
									if (thing.Data is Link)
									{
										linkIds.Add(((Link)thing.Data).Id);
										var viewModel = new LinkViewModel(this, thing.Data as Link);
										linkViewModels.Add(viewModel);
										Links.Add(viewModel);
									}
								}
								var linkMetadata = (await SnooStreamViewModel.OfflineService.GetLinkMetadata(linkIds)).ToList();
								for (int i = 0; i < linkMetadata.Count; i++)
								{
									linkViewModels[i].UpdateMetadata(linkMetadata[i]);
								}
								LastLinkId = postListing.Data.After;
							});
                    }
                });
            
            //clear the loading task when we're done
            _loadingTask = null;
        }

		public LinkViewModel CurrentSelected { get; set; }

		internal SubredditInit Dump()
		{
			return new SubredditInit
			{
				DefaultSort = Sort,
				Thing = Thing,
				Links = Links.Select(vm => vm.Link).ToList(),
				LastRefresh = LastRefresh
			};
		}

		public void MaybeRefresh()
		{
			if (Links.Count == 0)
				LoadMore();
			if(LastRefresh == null || (DateTime.Now - LastRefresh.Value).TotalMinutes > 30)
				Refresh();
			
		}

		public async void Refresh()
		{
			await RefreshTask();
		}

		private async Task RefreshImpl()
		{
			await SnooStreamViewModel.NotificationService.Report("refreshing posts", async () =>
			{
				var postListing = await SnooStreamViewModel.RedditService.GetAdditionalFromListing(Thing.Url + ".json?sort=" + Sort, LastLinkId);
				if (postListing != null)
				{
					LastRefresh = DateTime.Now;
					var linkIds = new List<string>();
					var replace = new List<Tuple<int, LinkViewModel>>();
					var move = new List<Tuple<int, int, LinkViewModel>>();
					var existing = new Dictionary<string, Tuple<int, LinkViewModel>>();
					var update = new List<LinkViewModel>();
					for (int i = 0; i < postListing.Data.Children.Count; i++)
					{
						var thing = postListing.Data.Children[i];
						if (thing.Data is Link)
						{
							linkIds.Add(((Link)thing.Data).Id);
							var viewModel = new LinkViewModel(this, thing.Data as Link);
							replace.Add(Tuple.Create(i, viewModel));
						}
					}

					for(int i = 0; i < Links.Count; i++)
					{
						if(!existing.ContainsKey(Links[i].Link.Id))
							existing.Add(Links[i].Link.Id, Tuple.Create(i, Links[i]));
					}

					foreach (var link in replace)
					{
						if (existing.ContainsKey(link.Item2.Link.Id))
						{
							var existingIndex = existing[link.Item2.Link.Id].Item1;
							if (existingIndex == link.Item1)
								update.Add(link.Item2);
							else
								move.Add(Tuple.Create(existingIndex, link.Item1, link.Item2));
						}
					}
					replace.RemoveAll((tpl) => update.Contains(tpl.Item2) || move.Any(tpl2 => tpl2.Item3 == tpl.Item2));

					SnooStreamViewModel.SystemServices.RunUIAsync(async () =>
					{
						foreach (var link in update)
						{
							existing[link.Link.Id].Item2.MergeLink(link.Link);
						}

						foreach(var linkTpl in move)
						{
							existing[linkTpl.Item3.Link.Id].Item2.MergeLink(linkTpl.Item3.Link);
						}

						bool unfinished = true;
						while(unfinished)
						{
							unfinished = false;
							foreach(var linkTpl in move.OrderBy(tpl => tpl.Item2))
							{
								var currentIndex = Links.IndexOf(linkTpl.Item3);
								if (currentIndex > 0 && currentIndex != linkTpl.Item2)
								{
									unfinished = true;
									Links.Move(currentIndex, linkTpl.Item2);
								}
							}
						}

						foreach (var newLink in replace)
						{
							Links[newLink.Item1] = newLink.Item2;
						}

						for(int i = Links.Count - 1; i > linkIds.Count; i--)
						{
							Links.RemoveAt(i);
						}

						var linkMetadata = (await SnooStreamViewModel.OfflineService.GetLinkMetadata(linkIds)).ToList();
						for (int i = 0; i < linkMetadata.Count; i++)
						{
							Links[i].UpdateMetadata(linkMetadata[i]);
						}
						LastLinkId = postListing.Data.After;
					});
				}
			});

			//clear the loading task when we're done
			_loadingTask = null;
		}
	}
}
