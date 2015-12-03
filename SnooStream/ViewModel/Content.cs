using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Windows.Foundation;
using SnooStream.Model;
using CommonResourceAcquisition.VideoAcquisition;
using CommonResourceAcquisition.ImageAcquisition;
using NBoilerpipePortable.Extractors;
using NBoilerpipePortable.Util;

namespace SnooStream.ViewModel
{
    public interface IContentRiverContext
    {
        bool HasAdditional { get; }
        Task<IEnumerable<object>> LoadMore(IProgress<float> progress, CancellationToken token);
        void NavigateToWeb(string url);
        void NavigateToComments(string contextUrl);
        bool MakeCollectionViewSource { get; }
        Task<ICommentBuilderContext> MakeCommentContext(string commentUrl);
        void SetCurrent(int index);
    }

    public class ContentRiverViewModel
    {
        public IContentRiverContext Context { get; set; }
        public ObservableCollection<object> ContentItems { get; set; }
        public CollectionViewSource ViewSource { get; set; }

        public ContentRiverViewModel() { }

        public ContentRiverViewModel(IContentRiverContext context)
        {
            Context = context;
            var contentCollection = new ContentCollection { Context = context };
            ViewSource = new CollectionViewSource { Source = ContentItems };
            ViewSource.View.CurrentChanged += View_CurrentChanged;
            contentCollection.CollectionView = ViewSource.View;
            ContentItems = contentCollection;
        }

        private async void View_CurrentChanged(object sender, object e)
        {
            var collectionView = sender as ICollectionView;
            await ContentBuilder.SetCurrentContent(collectionView.CurrentPosition, collectionView, Context);
            Context.SetCurrent(collectionView?.CurrentPosition ?? -1);
        }
    }

    //content builder
    // deal with focus/bound for the entire collection
    // embed content creation/loading/cancel/progress into IContentRiverContext/Builder
    public class ContentBuilder
    {
        //if we know what the content is make its view model
        //if we need to call an API first, make it a LoadViewModel
        public static object MakeContentViewModel(string url, string title, VotableViewModel votable, 
            LinkViewModel linkViewModel, IContentRiverContext context, ICollectionView contentView, INetworkLayer networkLayer)
        {
            object result = null;
            string targetHost = null;
            string fileName = null;

            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                var uri = new Uri(url);
                targetHost = uri.DnsSafeHost.ToLower();
                fileName = uri.AbsolutePath;
            }

            var glyph = LinkGlyphUtility.GetLinkGlyph((linkViewModel as object) ?? (url as object));

            if (linkViewModel != null && ((LinkViewModel)linkViewModel).Thing.IsSelf)
            {
                //make comments view model from the link viewmodel
                throw new NotImplementedException();
            }
            //needs to return a CommentsViewModel for a comment/commentspage
            //AboutSubreddit for a subreddit
            //AboutUser for a user
            //AboutSubreddit for a user multi reddit
            else if (LinkGlyphUtility.IsComment(url) ||
                LinkGlyphUtility.IsCommentsPage(url))
            {
                result = MakeSelfReplacingLoadViewModel(async (progress, token) =>
                {
                    var commentContext = await context.MakeCommentContext(url);
                    return commentContext.Comments;
                }, contentView);
            }
            else if (LinkGlyphUtility.IsSubreddit(url) || LinkGlyphUtility.IsUserMultiReddit(url))
            {
            }
            else if (LinkGlyphUtility.IsUser(url))
            {
                throw new NotImplementedException();
            }
            else if (fileName != null &&
                (fileName.EndsWith(".mp4") ||
                fileName.EndsWith(".gifv")))
            {
                result = new VideoContentViewModel { Url = url, Votable = votable, Context = context, Title = title, HasComments = linkViewModel != null };
            }
            else if (targetHost == "www.youtube.com" ||
                targetHost == "www.youtu.be" ||
                targetHost == "youtu.be" ||
                targetHost == "youtube.com" ||
                targetHost == "m.youtube.com" ||
                targetHost == "vimeo.com" ||
                targetHost == "www.vimeo.com" ||
                targetHost == "liveleak.com" ||
                targetHost == "www.liveleak.com" ||
                targetHost == "zippy.gfycat.com" ||
                targetHost == "fat.gfycat.com" ||
                targetHost == "giant.gfycat.com" ||
                targetHost == "www.gfycat.com" ||
                targetHost == "gfycat.com")
            {
                if (VideoAcquisition.IsAPI(url))
                    result = MakeSelfReplacingLoadViewModel(async (progress, token) =>
                    {
                        var videoResult = VideoAcquisition.GetVideo(url, networkLayer as IResourceNetworkLayer);
                        return new VideoContentViewModel
                        {
                            Context = context,
                            HasComments = linkViewModel != null,
                            Title = title,
                            Url = url,
                            Votable = votable,
                            PreviewUrl = await videoResult.PreviewUrl(networkLayer as IResourceNetworkLayer, progress, token),
                            PlayableStreams = await videoResult.PlayableStreams(networkLayer as IResourceNetworkLayer, progress, token)
                        };
                    }, contentView);
                else
                    result = ContentBuilder.MakeWebContent(url, title, contentView, networkLayer, context, linkViewModel, votable);

            }
            else
            {
                var imageAPIType = ImageAcquisition.ImageAPIType(url);
                if (imageAPIType == ImageAPIType.Async)
                {
                    //create LoadViewModel until we have the api part done
                    result = MakeSelfReplacingLoadViewModel(async (progress, token) =>
                    {
                        var imageResult = await ImageAcquisition.GetImagesFromUrl(title, url, networkLayer as IResourceNetworkLayer, progress, token);
                        if (imageResult.Count() > 1)
                        {
                            var itemContentViewSource = context.MakeCollectionViewSource ? new CollectionViewSource() : null;
                            var contentItems = new ObservableCollection<object>(imageResult.Select(tpl => MakeContentViewModel(tpl.Item2, tpl.Item1, null, null, context, itemContentViewSource?.View, networkLayer)));
                            return new ContentContainerViewModel
                            {
                                Url = url,
                                Title = title,
                                Context = context,
                                HasComments = linkViewModel != null,
                                Votable = votable,
                                ContentItems = contentItems,
                                ViewSource = itemContentViewSource
                            };
                        }
                        else
                        {
                            return new ImageContentViewModel { Url = imageResult.First().Item2, Title = title, HasComments = linkViewModel != null, Context = context, Votable = votable };
                        }
                    }, contentView);
                }
                else if (imageAPIType == ImageAPIType.Simple)
                {
                    result = new ImageContentViewModel { Url = ImageAcquisition.GetSimpleImageFromUrl(url), Title = title, HasComments = linkViewModel != null, Context = context, Votable = votable };
                }
                else
                {
                    //create LoadViewModel until we have at least the first part of the web data returned
                    //then replace it with a ContentCollectionViewModel comprised of TextContentViewModel and ImageContentViewModels
                    result = ContentBuilder.MakeWebContent(url, title, contentView, networkLayer, context, linkViewModel, votable);
                }
            }
            return result;
        }

        public static LoadViewModel MakeSelfReplacingLoadViewModel(Func<IProgress<float>, CancellationToken, Task<object>> viewModelMaker, ICollectionView collectionView)
        {
            LoadViewModel loadViewModel = null;
            loadViewModel = new LoadViewModel
            {
                LoadAction = async (progress, token) =>
                {
                    var replacementViewModel = await viewModelMaker(progress, token);
                    var itemIndex = collectionView.IndexOf(loadViewModel);
                    collectionView[itemIndex] = replacementViewModel;
                    SetCurrentContentItem(collectionView.CurrentPosition, collectionView, itemIndex);
                }
            };
            return loadViewModel;
        }

        public static LoadViewModel MakeWebContent(string url, string title, ICollectionView collectionView, INetworkLayer networkLayer, IContentRiverContext context, LinkViewModel linkViewModel, VotableViewModel votable)
        {
            return MakeSelfReplacingLoadViewModel(async (progress, token) =>
            {
                var itemContentViewSource = context.MakeCollectionViewSource ? new CollectionViewSource() : null;
                var webContent = await LoadWebContent(networkLayer, url, progress, token, context);
                var contentItems = new WebContentCollection(webContent.Item3, webContent.Item2, url, networkLayer, context);
                return new ContentContainerViewModel
                {
                    Url = url,
                    Title = title,
                    Context = context,
                    HasComments = linkViewModel != null,
                    Votable = votable,
                    ContentItems = contentItems,
                    ViewSource = itemContentViewSource
                };
            }, collectionView);
        }

        public static async Task<Tuple<string, string, IEnumerable<object>>> LoadWebContent(INetworkLayer networkLayer, string url, IProgress<float> progress, CancellationToken token, IContentRiverContext context)
        {
            return await Task.Run(async () =>
            {
                var result = new List<object>();
                string domain = url;
                if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                    domain = new Uri(url).Authority;

                var page = await networkLayer.CacheableGet(url, token, progress, null);
                if (!string.IsNullOrWhiteSpace(page))
                {
                    string title;
                    var pageBlocks = ArticleExtractor.INSTANCE.GetTextAndImageBlocks(page, new Uri(url), out title);
                    foreach (var tpl in pageBlocks)
                    {
                        if (!string.IsNullOrEmpty(tpl.Item2))
                        {
                            result.Add(new ImageContentViewModel { Url = tpl.Item2, Context = context });
                        }

                        if (!string.IsNullOrEmpty(tpl.Item1))
                        {
                            result.Add(new TextContentViewModel { Url = tpl.Item2, Text = tpl.Item1 });
                        }
                    }
                    var nextPageUrl = MultiPageUtils.FindNextPageLink(SgmlDomBuilder.GetBody(SgmlDomBuilder.BuildDocument(page)), url);
                    return Tuple.Create(nextPageUrl, title, (IEnumerable<object>)result);
                }
                return Tuple.Create("", "", (IEnumerable<object>)result);
            });
        }

        //awaits in this method need to be trailing/chained otherwise they might get called multiple times
        public static async Task SetCurrentContent(int index, ICollectionView contentView, IContentRiverContext context)
        {
            //look through the collection and defocus/unbind any ContentViewModels that are not near enough to the current index
            for (int i = 0; i < contentView.Count; i++)
            {
                SetCurrentContentItem(index, contentView, i);
            }

            //we're nearing the end of the loaded collection
            if (contentView.Count < index + 5)
            {
                //only try to load if we know we can get more items
                if (contentView.HasMoreItems)
                {
                    await contentView.LoadMoreItemsAsync(20);
                }
            }
        }

        private static void SetCurrentContentItem(int index, ICollectionView contentView, int i)
        {
            //check if we're focused, bound or unbound
            var distanceFromFocus = Math.Abs(index - i);
            var content = contentView[i] as ContentViewModel;
            if (content != null)
            {
                if (distanceFromFocus == 0)
                {
                    content.Focused = true;
                    content.Bound = true;
                }
                else if (distanceFromFocus < 3)
                {
                    content.Bound = true;
                    content.Focused = false;
                }
                else
                {
                    content.Bound = false;
                    content.Focused = false;
                }
            }
            //kick off the loader for the item, but not if its the collection load placeholder
            else if (contentView[i] is LoadViewModel && ((LoadViewModel)contentView[i]).Kind == LoadKind.Item)
            {
                var loadViewModel = contentView[i] as LoadViewModel;
                if (distanceFromFocus < 5)
                {
                    if (loadViewModel.State == LoadState.None || loadViewModel.State == LoadState.Cancelled)
                    {
                        loadViewModel.Load();
                    }
                }
                else if (loadViewModel.State == LoadState.Loading)
                {
                    ((LoadViewModel)contentView[i]).Cancel();
                }
            }
        }
    }

    class WebContentCollection : ObservableCollection<object>, ISupportIncrementalLoading
    {
        INetworkLayer _networkLayer;
        IContentRiverContext _context;
        public ICollectionView CollectionView { get; set; }
        string _nextUrl;
        string _originalUrl;
        public WebContentCollection(IEnumerable<object> initial, string nextUrl, string originalUrl, INetworkLayer networkLayer, IContentRiverContext context) : base(initial)
        {
            _networkLayer = networkLayer;
            _context = context;
            _nextUrl = nextUrl;
            _originalUrl = originalUrl;
        }

        public bool HasMoreItems
        {
            get
            {
                return _nextUrl != null;
            }
        }

        Task<LoadMoreItemsResult> _loadTask;

        async Task<LoadMoreItemsResult> LoadMoreItems()
        {
            try
            {
                var oldCount = Count;
                var loadViewModel = new LoadViewModel
                {
                    Kind = LoadKind.Collection,
                    IsCritical = false,
                    LoadAction = async (progress, token) =>
                    {
                        var loadedContent = await ContentBuilder.LoadWebContent(_networkLayer, _nextUrl, progress, token, _context);
                        var first = loadedContent.Item3.FirstOrDefault() as ContentViewModel;
                        bool replacingCurrent = CollectionView.CurrentPosition == Count - 1;
                        this[Count - 1] = first;

                        if (replacingCurrent && first != null)
                        {
                            //switchback from LoadViewModel to ContentViewModel needs to be Current aware so it can reset the focus
                            first.Focused = true;
                        }

                        foreach (var remaining in loadedContent.Item3.Skip(1))
                            Add(remaining);

                        if (loadedContent.Item2 != _nextUrl)
                            _nextUrl = loadedContent.Item2;
                    }
                };
                Add(loadViewModel);
                await loadViewModel.LoadAsync();
                return new LoadMoreItemsResult { Count = (uint)(Count - oldCount) };
            }
            finally
            {
                //wipe out the old load task so we can do it again
                _loadTask = null;
            }
        }

        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            if (_loadTask == null)
            {
                lock (this)
                {
                    if (_loadTask == null)
                    {
                        _loadTask = LoadMoreItems();
                    }
                }
            }

            return _loadTask.AsAsyncOperation();
        }
    }

    class ContentCollection : ObservableCollection<object>, ISupportIncrementalLoading
    {
        public IContentRiverContext Context { get; set; }
        public ICollectionView CollectionView { get; set; }
        public bool HasMoreItems
        {
            get
            {
                return Context.HasAdditional;
            }
        }

        Task<LoadMoreItemsResult> _loadTask;

        async Task<LoadMoreItemsResult> LoadMoreItems()
        {
            try
            {
                var oldCount = Count;
                var loadViewModel = new LoadViewModel
                {
                    Kind = LoadKind.Collection,
                    IsCritical = false,
                    LoadAction = async (progress, token) =>
                    {
                        var loadedContent = await Context.LoadMore(progress, token);
                        var first = loadedContent.FirstOrDefault() as ContentViewModel;
                        bool replacingCurrent = CollectionView.CurrentPosition == Count - 1;
                        this[Count - 1] = first;

                        if (replacingCurrent && first != null)
                        {
                            //switchback from LoadViewModel to ContentViewModel needs to be Current aware so it can reset the focus
                            first.Focused = true;
                        }

                        foreach (var remaining in loadedContent.Skip(1))
                            Add(remaining);
                    }
                };
                Add(loadViewModel);
                await loadViewModel.LoadAsync();
                return new LoadMoreItemsResult { Count = (uint)(Count - oldCount) };
            }
            finally
            {
                //wipe out the old load task so we can do it again
                _loadTask = null;
            }
        }

        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            if (_loadTask == null)
            {
                lock (this)
                {
                    if (_loadTask == null)
                    {
                        _loadTask = LoadMoreItems();
                    }
                }
            }

            return _loadTask.AsAsyncOperation();
        }
    }

    public class ContentViewModel : ObservableObject
    {
        public IContentRiverContext Context { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public bool Focused { get; set; }
        public bool Bound { get; set; }
        public bool HasComments { get; set; }
        public VotableViewModel Votable { get; set; }

        public void NavigateToWeb()
        {
            Context.NavigateToWeb(Url);
        }

        public void NavigateToComments()
        {
            Context.NavigateToComments(Url);
        }
    }

    public class TextContentViewModel : ContentViewModel
    {
        public string Text { get; set; }
    }

    public class ImageContentViewModel : ContentViewModel
    {

    }

    public class VideoContentViewModel : ContentViewModel
    {
        public string PreviewUrl { get; set; }
        public IEnumerable<Tuple<string, string>> PlayableStreams { get; set; }
    }

    public class ContentContainerViewModel : ContentViewModel
    {
        public ObservableCollection<object> ContentItems { get; set; }
        public CollectionViewSource ViewSource { get; set; }
    }

    public class LinkContentRiverContext : IContentRiverContext
    {
        public INetworkLayer NetworkLayer { get; set; }
        public LinkRiverViewModel LinkRiverContext { get; set; }
        public ICollectionView ContentView { get; set; }
        public bool HasAdditional
        {
            get
            {
                return LinkRiverContext.Context.HasAdditional;
            }
        }

        public bool MakeCollectionViewSource
        {
            get
            {
                return true;
            }
        }


        public async Task<IEnumerable<object>> LoadMore(IProgress<float> progress, CancellationToken token)
        {
            var additionalListing = await LinkRiverContext.LoadAdditionalAsync(progress, token);
            return additionalListing.Select(link => ContentBuilder.MakeContentViewModel(link.Thing.Url, link.Thing.Title, link.Votable, link, this, ContentView, NetworkLayer));
        }

        public void NavigateToComments(string contextUrl)
        {
            throw new NotImplementedException();
        }

        public void NavigateToWeb(string url)
        {
            throw new NotImplementedException();
        }

        public Task<ICommentBuilderContext> MakeCommentContext(string commentUrl)
        {
            throw new NotImplementedException();
        }

        public void SetCurrent(int index)
        {
            LinkRiverContext.LinkViewSource.View.MoveCurrentToPosition(index);
        }
    }

    public class CommentContentRiverContext : IContentRiverContext
    {
        bool _initialLoaded = false;
        public bool HasAdditional
        {
            get
            {
                return !_initialLoaded;
            }
        }

        public CommentsViewModel Comments { get; set; }
        public INetworkLayer NetworkLayer { get; set; }
        public ICollectionView CollectionView { get; set; }

        public bool MakeCollectionViewSource
        {
            get
            {
                return true;
            }
        }

        public async Task<IEnumerable<object>> LoadMore(IProgress<float> progress, CancellationToken token)
        {
            _initialLoaded = true;
            List<object> result = new List<object>();
            foreach (var commentObj in Comments.Comments)
            {
                var comment = commentObj as CommentViewModel;
                if (comment != null)
                {
                    var bodyMD = await comment.BodyMDTask;
                    foreach (var link in bodyMD.GetLinks())
                    {
                        result.Add(ContentBuilder.MakeContentViewModel(link.Key, link.Value, comment.Votable, null, this, CollectionView, NetworkLayer));
                    }
                }
            }
            return result;
        }

        public void NavigateToComments(string contextUrl)
        {
            //same as pressing back button
        }

        public void NavigateToWeb(string url)
        {
            //goto reddit url
        }

        public Task<ICommentBuilderContext> MakeCommentContext(string commentUrl)
        {
            throw new NotImplementedException();
        }

        public void SetCurrent(int index)
        {
            //set the focused item from the comment context we came from
        }
    }
}
