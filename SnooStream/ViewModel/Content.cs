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
using Windows.UI.Xaml;
using SnooStream.Common;
using System.Net;
using SnooStream.Controls;

namespace SnooStream.ViewModel
{
    public interface IContentRiverContext
    {
        bool HasAdditional { get; }
        IEnumerable<object> LoadInitial();
        Task<IEnumerable<object>> LoadMore(IProgress<float> progress, CancellationToken token);
        void NavigateToWeb(string url);
        void NavigateToComments(string contextUrl);
        LinkViewModel CurrentLink();
        bool MakeCollectionViewSource { get; }
        Task<ICommentBuilderContext> MakeCommentContext(string commentUrl, IProgress<float> progress, CancellationToken token);
        INavigationContext NavigationContext { get; }
        int Current { get; set; }
        ObservableCollection<IHubNavCommand> MakeHubNavCommands(ContentRiverViewModel contentRiverViewModel);
        void UpdateHubNavCommands(ContentRiverViewModel contentRiverViewModel);
    }

    public class ContentRiverViewModel : IHasHubNavCommands
    {
        public IContentRiverContext Context { get; set; }
        public LoadItemCollectionBase ContentItems { get; set; }
        public IEnumerable<IHubNavCommand> Commands { get; set; }

        public ContentRiverViewModel() { }

        public ContentRiverViewModel(IContentRiverContext context)
        {
            Context = context;
            var contentCollection = new ContentCollection(context);
            contentCollection.CurrentChanged += View_CurrentChanged;
            ContentItems = contentCollection;
            Commands = context.MakeHubNavCommands(this);
        }

        private async void View_CurrentChanged(object sender, object e)
        {
            var collectionView = sender as ICollectionView;
            await ContentBuilder.SetCurrentContent(collectionView.CurrentPosition, collectionView, Context);
            Context.Current = collectionView?.CurrentPosition ?? -1;
            Context.UpdateHubNavCommands(this);
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
            LinkViewModel linkViewModel, IContentRiverContext context, ICollectionView contentView, INetworkLayer networkLayer, ObservableCollection<object> collection)
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
                result = MakeSelfReplacingLoadViewModel(async (progress, token) =>
                {
                    var commentContext = await context.MakeCommentContext(url, progress, token);
                    return commentContext.Comments;
                }, contentView, collection);
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
                    var commentContext = await context.MakeCommentContext(url, progress, token);
                    return commentContext.Comments;
                }, contentView, collection);
            }
            else if (LinkGlyphUtility.IsSubreddit(url) || LinkGlyphUtility.IsUserMultiReddit(url))
            {
                result = context.NavigationContext.MakeLinkRiverContext(url, null, null);
            }
            else if (LinkGlyphUtility.IsUser(url))
            {
                context.NavigationContext.MakeUserDetailsContext(url);
            }
            else if (fileName != null &&
                (fileName.EndsWith(".mp4") ||
                fileName.EndsWith(".gifv")))
            {
                //only set looping if we're looking at a gif or a gif'alike 
                result = new VideoContentViewModel { IsLooping = VideoAcquisition.IsGifType(url), PlayableStreams = new List<Tuple<string, string>> { new Tuple<string, string>(url.Replace(".gifv", ".mp4"), title) }, Url = url, Votable = votable, Context = context, Title = title, HasComments = linkViewModel != null };
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
                    }, contentView, collection);
                else
                    result = ContentBuilder.MakeWebContent(url, title, contentView, networkLayer, context, linkViewModel, votable, collection);

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
                            var contentItems = new RangeCollection();
                            foreach (var vm in imageResult.Select(tpl => MakeContentViewModel(tpl.Item2, tpl.Item1, null, null, context, contentItems, networkLayer, collection)))
                            {
                                contentItems.Add(vm);
                            }

                            return new ContentContainerViewModel
                            {
                                SingleViewItem = true,
                                Url = url,
                                Title = title,
                                Context = context,
                                HasComments = linkViewModel != null,
                                Votable = votable,
                                ContentItems = contentItems
                            };
                        }
                        else
                        {
                            return new ImageContentViewModel { Url = imageResult.First().Item2, Title = title, HasComments = linkViewModel != null, Context = context, Votable = votable };
                        }
                    }, contentView, collection);
                }
                else if (imageAPIType == ImageAPIType.Simple)
                {
                    result = new ImageContentViewModel { Url = ImageAcquisition.GetSimpleImageFromUrl(url), Title = title, HasComments = linkViewModel != null, Context = context, Votable = votable };
                }
                else
                {
                    //create LoadViewModel until we have at least the first part of the web data returned
                    //then replace it with a ContentCollectionViewModel comprised of TextContentViewModel and ImageContentViewModels
                    result = ContentBuilder.MakeWebContent(url, title, contentView, networkLayer, context, linkViewModel, votable, collection);
                }
            }
            return result;
        }

        public static LoadViewModel MakeSelfReplacingLoadViewModel(Func<IProgress<float>, CancellationToken, Task<object>> viewModelMaker, ICollectionView collectionView, ObservableCollection<object> collection)
        {
            if (collectionView == null)
                throw new ArgumentNullException();

            LoadViewModel loadViewModel = null;
            loadViewModel = new LoadViewModel
            {
                LoadAction = async (progress, token) =>
                {
                    var replacementViewModel = await viewModelMaker(progress, token);
                    if(Window.Current != null && Window.Current.Dispatcher != null)
                        Window.Current.Dispatcher.CurrentPriority = Windows.UI.Core.CoreDispatcherPriority.Idle;

                    await Task.Yield();

                    var itemIndex = collection.IndexOf(loadViewModel);
                    if (collection is ContentCollection)
                    {
                        await ((ContentCollection)collection).BlockingReplace(itemIndex, replacementViewModel);
                    }
                    else
                    {
                        collection[itemIndex] = replacementViewModel;
                    }
                    
                    SetCurrentContentItem(collectionView.CurrentPosition, collectionView, itemIndex);
                }
            };
            return loadViewModel;
        }

        public static LoadViewModel MakeWebContent(string url, string title, ICollectionView collectionView, INetworkLayer networkLayer, IContentRiverContext context, LinkViewModel linkViewModel, VotableViewModel votable, ObservableCollection<object> collection)
        {
            return MakeSelfReplacingLoadViewModel(async (progress, token) =>
            {
                var webContent = await LoadWebContent(networkLayer, url, progress, token, context);
                var contentItems = new WebContentCollection(webContent.Item3, webContent.Item1, url, networkLayer, context);
                return new ContentContainerViewModel
                {
                    Url = url,
                    Title = webContent.Item2 ?? title,
                    Context = context,
                    HasComments = linkViewModel != null,
                    Votable = votable,
                    ContentItems = contentItems
                };
            }, collectionView, collection);
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
                    if (Uri.IsWellFormedUriString(nextPageUrl, UriKind.RelativeOrAbsolute))
                        return Tuple.Create(nextPageUrl, title, (IEnumerable<object>)result);
                    else
                        return Tuple.Create<string, string, IEnumerable<object>>(null, title, (IEnumerable<object>)result);
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
                else if (loadViewModel.State == LoadState.Loading || loadViewModel.State == LoadState.Refreshing)
                {
                    ((LoadViewModel)contentView[i]).Cancel();
                }
            }
        }
    }

    class WebContentCollection : LoadItemCollectionBase
    {
        INetworkLayer _networkLayer;
        IContentRiverContext _context;
        public ICollectionView CollectionView { get; set; }
        string _nextUrl;
        string _originalUrl;
        public WebContentCollection(IEnumerable<object> initial, string nextUrl, string originalUrl, INetworkLayer networkLayer, IContentRiverContext context)
        {
            _networkLayer = networkLayer;
            _context = context;
            _nextUrl = nextUrl;
            _originalUrl = originalUrl;
            AddRange(initial);
            HasLoaded = true;
        }

        public override bool HasMoreItems
        {
            get
            {
                return base.HasMoreItems && _nextUrl != null;
            }
        }

        async Task LoadImpl(IProgress<float> progress, CancellationToken token)
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
                _nextUrl = loadedContent.Item1;
        }

        protected override Task Refresh(IProgress<float> progress, CancellationToken token)
        {
            _nextUrl = null;
            return LoadImpl(progress, token);
        }

        protected override Task LoadInitial(IProgress<float> progress, CancellationToken token)
        {
            return LoadImpl(progress, token);
        }

        protected override Task LoadAdditional(IProgress<float> progress, CancellationToken token)
        {
            return LoadImpl(progress, token);
        }
    }

    class ContentCollection : LoadItemCollectionBase
    {
        public IContentRiverContext Context { get; set; }

        public ContentCollection(IContentRiverContext context)
        {
            Context = context;
        }

        public override bool HasMoreItems
        {
            get
            {
                return base.HasMoreItems && Context.HasAdditional;
            }
        }

        async Task LoadImpl(IProgress<float> progress, CancellationToken token)
        {
            var loadedContent = await Context.LoadMore(progress, token);
            var first = loadedContent.FirstOrDefault() as ContentViewModel;
            bool replacingCurrent = CurrentPosition == Count - 1;
            this[Count - 1] = first;

            if (replacingCurrent && first != null)
            {
                //switchback from LoadViewModel to ContentViewModel needs to be Current aware so it can reset the focus
                first.Focused = true;
            }

            foreach (var remaining in loadedContent.Skip(1))
                Add(remaining);
        }
        
        public async Task BlockingReplace(int index, object value)
        {
            bool notFinished = true;
            while (notFinished)
            {
                try
                {
                    using (BlockReentrancy())
                    {
                        SetItem(index, value);
                        notFinished = false;
                    }
                }
                catch
                {

                }
                await Task.Yield();
            }
        }

        protected override Task Refresh(IProgress<float> progress, CancellationToken token)
        {
            AddRange(Context.LoadInitial());
            return Task.FromResult(true);
        }

        protected override Task LoadInitial(IProgress<float> progress, CancellationToken token)
        {
            return LoadImpl(progress, token);
        }

        protected override Task LoadAdditional(IProgress<float> progress, CancellationToken token)
        {
            return LoadImpl(progress, token);
        }
    }

    public class ContentViewModel : SnooObservableObject
    {
        public IContentRiverContext Context { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public virtual bool Focused { get; set; }
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
        private bool _isPlaying;
        public override bool Focused { get { return base.Focused; } set { base.Focused = value; IsPlaying = value; } }
        public bool IsPlaying { get { return _isPlaying; } set { Set("IsPlaying", ref _isPlaying, value); } }
        public double PlayPosition { get; set; }
        public bool IsLooping { get; set; }
        public string PreviewUrl { get; set; }
        public string PlayableUrl
        {
            get
            {
                return PlayableStreams?.FirstOrDefault()?.Item1;
            }
        }
        public IEnumerable<Tuple<string, string>> PlayableStreams { get; set; }
    }

    public class ContentContainerViewModel : ContentViewModel
    {
        public bool SingleViewItem { get; set; }
        public RangedCollectionBase ContentItems { get; set; }
    }

    public abstract class ContentRiverBaseContext : IContentRiverContext
    {
        public INavigationContext NavigationContext { get; set; }
        public abstract bool HasAdditional { get; }
        public abstract bool MakeCollectionViewSource { get; }
        public abstract int Current { get; set; }

        ObservableCollection<IHubNavCommand> _hubNavCommands;
        public ObservableCollection<IHubNavCommand> MakeHubNavCommands(ContentRiverViewModel contentRiverViewModel)
        {
            if (_hubNavCommands == null)
            {
                _hubNavCommands = new ObservableCollection<IHubNavCommand>
                {
                    new DelayedLaunchUri { TargetUrl = () => (contentRiverViewModel.ContentItems.CurrentItem as ContentViewModel)?.Url, NavigationContext = NavigationContext },
                    new DelayedVoteNavCommand(() => (contentRiverViewModel.ContentItems.CurrentItem as ContentViewModel)?.Votable) { NavigationContext = NavigationContext },
                };
            }

            UpdateHubNavCommands(contentRiverViewModel);
            return _hubNavCommands;
        }

        public void UpdateHubNavCommands(ContentRiverViewModel contentRiverViewModel)
        {
            var currentItem = contentRiverViewModel.ContentItems.CurrentItem;

            var existingGalleryCommand = _hubNavCommands.OfType<DelayedGalleryCommand>().FirstOrDefault();
            var existingRefreshCommand = _hubNavCommands.OfType<DelayedRefreshNavCommand>().FirstOrDefault();
            var existingCommentsCommand = _hubNavCommands.OfType<CommentsNavCommand>().FirstOrDefault();

            if (currentItem is ContentContainerViewModel && ((ContentContainerViewModel)currentItem).SingleViewItem)
            {
                if (existingGalleryCommand == null)
                    _hubNavCommands.Insert(0, new DelayedGalleryCommand(() => { return; }, (obj) => obj is ContentContainerViewModel, contentRiverViewModel.ContentItems) { NavigationContext = NavigationContext });
            }
            else if (existingGalleryCommand != null)
                _hubNavCommands.Remove(existingGalleryCommand);

            if (currentItem is CommentsViewModel || !(currentItem is ContentContainerViewModel && ((ContentContainerViewModel)currentItem).SingleViewItem))
            {
                if (existingRefreshCommand != null)
                    _hubNavCommands.Insert(0, new DelayedRefreshNavCommand(() => contentRiverViewModel.ContentItems.CurrentItem as IRefreshable, (obj) => obj is IRefreshable, contentRiverViewModel.ContentItems) { NavigationContext = NavigationContext });
            }
            else if (existingRefreshCommand != null)
                _hubNavCommands.Remove(existingRefreshCommand);


            if ((!(currentItem is CommentsViewModel) || (currentItem is ContentContainerViewModel && ((ContentContainerViewModel)currentItem).SingleViewItem)) && CurrentLink() != null)
            {
                if (existingCommentsCommand == null)
                    _hubNavCommands.Insert(0, new CommentsNavCommand(CurrentLink, (obj) => CurrentLink() != null, contentRiverViewModel.ContentItems) { NavigationContext = NavigationContext });
            }
            else if (existingGalleryCommand != null)
                _hubNavCommands.Remove(existingCommentsCommand);

        }

        public abstract IEnumerable<object> LoadInitial();
        public abstract Task<IEnumerable<object>> LoadMore(IProgress<float> progress, CancellationToken token);
        public abstract void NavigateToWeb(string url);
        public abstract void NavigateToComments(string contextUrl);
        public abstract Task<ICommentBuilderContext> MakeCommentContext(string commentUrl, IProgress<float> progress, CancellationToken token);
        public abstract LinkViewModel CurrentLink();
    }

    public class LinkContentRiverContext : ContentRiverBaseContext
    {
        public INetworkLayer NetworkLayer { get; set; }
        public LinkRiverViewModel LinkRiverContext { get; set; }
        public ICollectionView ContentView { get; set; }
        public ObservableCollection<object> Collection { get; set; }
        public override bool HasAdditional
        {
            get
            {
                return LinkRiverContext.Context.HasAdditional;
            }
        }

        public override bool MakeCollectionViewSource
        {
            get
            {
                return true;
            }
        }


        public override async Task<IEnumerable<object>> LoadMore(IProgress<float> progress, CancellationToken token)
        {
            var additionalListing = await LinkRiverContext.LoadAdditionalAsync(progress, token);
            return additionalListing.Select(link => ContentBuilder.MakeContentViewModel(link.Thing.Url, WebUtility.HtmlDecode(link.Thing.Title), link.Votable, link, this, ContentView, NetworkLayer, Collection));
        }

        public override void NavigateToComments(string contextUrl)
        {
            throw new NotImplementedException();
        }

        public override void NavigateToWeb(string url)
        {
            throw new NotImplementedException();
        }

        public override async Task<ICommentBuilderContext> MakeCommentContext(string commentUrl, IProgress<float> progress, CancellationToken token)
        {
            var comments = NavigationContext.MakeCommentContext(commentUrl, null, null, null);
            await comments.LoadAsync(progress, token);
            return comments.Context;
        }

        public override int Current
        {
            get
            {
                return LinkRiverContext.Links.CurrentPosition;
            }
            set
            {
                LinkRiverContext.Links.MoveCurrentToPosition(value);
            }
        }

        public override IEnumerable<object> LoadInitial()
        {
            return LinkRiverContext.Links.OfType<LinkViewModel>().Select(link => ContentBuilder.MakeContentViewModel(link.Thing.Url, WebUtility.HtmlDecode(link.Thing.Title), link.Votable, link, this, ContentView, NetworkLayer, Collection));
        }

        public override LinkViewModel CurrentLink()
        {
            return LinkRiverContext.Links.CurrentItem as LinkViewModel;
        }
    }

    public class CommentContentRiverContext : ContentRiverBaseContext
    {
        bool _initialLoaded = false;
        public override bool HasAdditional
        {
            get
            {
                return !_initialLoaded;
            }
        }

        public string InitialUrl { get; set; }
        public CommentsViewModel Comments { get; set; }
        public INetworkLayer NetworkLayer { get; set; }
        public ICollectionView CollectionView { get; set; }
        public ObservableCollection<object> Collection { get; set; }
        private Dictionary<int, CommentViewModel> _contentToCommentMap = new Dictionary<int, CommentViewModel>(); 
        public override bool MakeCollectionViewSource
        {
            get
            {
                return true;
            }
        }

        public override async Task<IEnumerable<object>> LoadMore(IProgress<float> progress, CancellationToken token)
        {
            _initialLoaded = true;
            List<object> result = new List<object>();
            await Comments.LoadState.LoadAsync();
            await Comments.Comments.LoadMoreItemsAsync(500).AsTask(token);
            foreach (var commentObj in Comments.Comments)
            {
                var comment = commentObj as CommentViewModel;
                if (comment != null)
                {
                    var body = comment.Body;
                    if (body != null)
                    {
                        var bodyMD = body as SnooDom.SnooDom;
                        if (bodyMD == null && body is String)
                        {
                            bodyMD = Comments.Context.MakeMarkdown(body as string);
                        }
                        if (bodyMD != null)
                        {
                            foreach (var link in bodyMD.GetLinks())
                            {
                                var contentViewModel = ContentBuilder.MakeContentViewModel(link.Key, link.Value, comment.Votable, null, this, CollectionView, NetworkLayer, Collection);
                                result.Add(contentViewModel);
                                _contentToCommentMap.Add(result.Count - 1, comment);

                                //only set it once
                                if (link.Key == InitialUrl && Current == -1)
                                {
                                    Current = result.Count - 1;
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }

        public override void NavigateToComments(string contextUrl)
        {
            //same as pressing back button
        }

        public override void NavigateToWeb(string url)
        {
            //goto reddit url
        }

        public override async Task<ICommentBuilderContext> MakeCommentContext(string commentUrl, IProgress<float> progress, CancellationToken token)
        {
            var madeViewModel = NavigationContext.MakeCommentContext(commentUrl, null, null, null);
            await madeViewModel.LoadAsync(progress, token);
            return madeViewModel.Context;
        }

        int _current = -1;
        public override int Current
        {
            get
            {
                return _current;
            }
            set
            {
                if (_contentToCommentMap.ContainsKey(value))
                {
                    var foundComment = _contentToCommentMap[value];
                    Comments.Comments.MoveCurrentToPosition(Comments.Comments.IndexOf(foundComment));
                }
                _current = value;
            }
        }

        public override IEnumerable<object> LoadInitial()
        {
            _initialLoaded = true;
            List<object> result = new List<object>();
            foreach (var commentObj in Comments.Comments)
            {
                var comment = commentObj as CommentViewModel;
                if (comment != null)
                {
                    var bodyMD = comment.Body as SnooDom.SnooDom;
                    if (bodyMD != null)
                    {
                        foreach (var link in bodyMD.GetLinks())
                        {
                            var contentViewModel = ContentBuilder.MakeContentViewModel(link.Key, link.Value, comment.Votable, null, this, CollectionView, NetworkLayer, Collection);
                            result.Add(contentViewModel);
                            _contentToCommentMap.Add(result.Count - 1, comment);

                            //only set it once
                            if (link.Key == InitialUrl && Current == -1)
                            {
                                Current = result.Count - 1;
                            }
                        }
                    }
                }
            }
            return result;
        }

        public override LinkViewModel CurrentLink()
        {
            throw new NotImplementedException();
        }
    }
}
