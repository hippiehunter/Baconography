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
    public class LinkRiverViewModel : ViewModelBase, ICollection<LinkViewModel>, INotifyCollectionChanged
    {
        //need to come up with an init blob setup for this, meaining a per river blob
        public Subreddit Thing { get; private set; }
        public string Sort { get; private set; }
        public bool Loading { get { return _loadingTask != null; } }
        private string LastLinkId { get; set; }
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
        public LinkRiverViewModel(bool isLocal, Subreddit thing, string sort, IEnumerable<Link> initialLinks)
        {
            IsLocal = isLocal;
            Thing = thing;
            Sort = sort ?? "hot";
            Links = new ObservableCollection<LinkViewModel>();
            if(initialLinks != null)
            {
                ProcessLinkThings(initialLinks);
            }

            _previewTask = new Lazy<Task<LinkStreamPreviewEnumerator>>(() =>
                {
                    var previewTask = PreviewTaskImpl();
                    previewTask.ContinueWith(tsk => RaisePropertyChanged("PreviewBinding"), SnooStreamViewModel.UIScheduler);
                    previewTask.ContinueWith(async tsk =>
                        {
                            var tskResult = tsk.TryValue();
                            if (tskResult != null)
                                await LinkStreamPreviewEnumerator.FillPreviewEnumerator(tskResult, 6, SnooStreamViewModel.UIContextCancellationToken);
                        });
                    return previewTask;
                });
        }

        public LinkStreamPreviewEnumerator PreviewBinding
        {
            get
            {
                return _previewTask.Value.TryValue();
            }
        }

        Lazy<Task<LinkStreamPreviewEnumerator>> _previewTask;
        private async Task<LinkStreamPreviewEnumerator> PreviewTaskImpl()
        {
            var enumerator = await LinkStreamPreviewEnumerator.MakePreviewEnumerator(this, new LinkStreamViewModel(this, null));
            return enumerator;
        }

        private void ProcessLinkThings(IEnumerable<Link> links)
        {
            foreach (var link in links)
            {
                Links.Add(new LinkViewModel(this, link));
            }
        }

        public ObservableCollection<LinkViewModel> Links { get; set; }
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

        public async Task LoadMoreImpl()
        {
            await SnooStreamViewModel.NotificationService.Report("loading posts", async () =>
                {
                    var postListing = LastLinkId != null ? 
                        await SnooStreamViewModel.RedditService.GetAdditionalFromListing(Thing.Url + ".json?sort=" + Sort, LastLinkId) :
                        await SnooStreamViewModel.RedditService.GetPostsBySubreddit(Thing.Url, Sort);

                    if (postListing != null)
                    {
                        await Task.Factory.StartNew(async () =>
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
                            }, SnooStreamViewModel.UIContextCancellationToken, TaskCreationOptions.PreferFairness, SnooStreamViewModel.UIScheduler);
                    }
                });
            
            //clear the loading task when we're done
            _loadingTask = null;
        }

        public void Add(LinkViewModel item)
        {
            Links.Add(item);
        }

        public void Clear()
        {
            Links.Clear();
        }

        public bool Contains(LinkViewModel item)
        {
            return Links.Contains(item);
        }

        public void CopyTo(LinkViewModel[] array, int arrayIndex)
        {
            Links.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return Links.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(LinkViewModel item)
        {
            return Links.Remove(item);
        }

        public IEnumerator<LinkViewModel> GetEnumerator()
        {
            return Links.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Links.GetEnumerator();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                Links.CollectionChanged += value;
            }
            remove
            {
                Links.CollectionChanged -= value;
            }
        }
    }
}
