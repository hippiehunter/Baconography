using SnooSharp;
using SnooStream.Controls;
using SnooStream.Converters;
using SnooStream.Model;
using SnooStream.Templates;
using SnooStream.ViewModel;
using SnooStreamBackground;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SnooStream.Common
{
    public interface INavigationContext
    {
        void LaunchUri(string uri);
        void Navigate(object viewModel);
        Task SaveState();
        ContentRiverViewModel MakeContentRiverContext(object context, string url);
        CommentsViewModel MakeCommentContext(string url, string focusId, string sort, LinkViewModel linkViewModel);
        LinkRiverViewModel MakeLinkRiverContext(string subreddit, string focusId, string sort);
        SearchViewModel MakeSearchContext(string query, string restrictedToSubreddit, bool subredditsOnly);
        UserViewModel MakeUserDetailsContext(string username);

        LoginViewModel LoginViewModel { get; }
        ActivitiesViewModel ActivitiesViewModel { get; }
        SelfViewModel SelfViewModel { get; }
        SettingsViewModel SettingsViewModel { get; }
        SubredditRiverViewModel SubredditRiver { get; }
        NavMenu NavMenu { get; }
        AdaptiveHubNav HubNav { get; set; }

        DataTemplate LinkRiverTemplate { get; }
        DataTemplate LoginTemplate { get; }
        DataTemplate ContentRiverTemplate { get; }
        DataTemplate CommentTemplate { get; }
        DataTemplate SubredditRiverTemplate { get; }
        DataTemplate SearchTemplate { get; }
        DataTemplate SelfTemplate { get; }
        DataTemplate UserDetailsTemplate { get; }
        DataTemplate SettingsTemplate { get; }
        DataTemplate OAuthLandingTemplate { get; }
        DataTemplate ContentSettingsTemplate { get; }

        IEnumerable<object> ViewModelStack { get; }

        Dictionary<string, object> MakePageState(ContentSettingsViewModel settings);
        Dictionary<string, object> MakePageState(SettingsViewModel settings);
        Dictionary<string, object> MakePageState(UserViewModel user);
        Dictionary<string, object> MakePageState(CommentsViewModel comments);
        Dictionary<string, object> MakePageState(LinkRiverViewModel links);
        Dictionary<string, object> MakePageState(ContentRiverViewModel contentRiver);
        Dictionary<string, object> MakePageState(SearchViewModel search);
        Dictionary<string, object> MakePageState(LoginViewModel login);
        Dictionary<string, object> MakePageState(SelfViewModel self);
        ILinkContext MakeLinkContext(string url);
    }

    public class Navigation
    {
        //Subreddit:
        public static Regex SubredditRegex = new Regex("(?:^|\\s|reddit.com)/r/[a-zA-Z0-9_.]+/?$");

        //Comments page:
        public static Regex CommentsPageRegex = new Regex("(?:^|\\s|reddit.com)/r/[a-zA-Z0-9_.]+/comments/[a-zA-Z0-9_]+/(?:[a-zA-Z0-9_]+/)*?");

        //Short URL comments page:
        public static Regex ShortCommentsPageRegex = new Regex("(?:redd.it)/[a-zA-Z0-9_.]+/?");

        //Comment:
        public static Regex CommentRegex = new Regex("(?:^|\\s|reddit.com)/r/[a-zA-Z0-9_.]+/comments/[a-zA-Z0-9_]+/[a-zA-Z0-9_]+/[a-zA-Z0-9_]+/?");

        //User Multireddit:
        public static Regex UserMultiredditRegex = new Regex("(?:^|\\s|reddit.com)/u(?:ser)*/[a-zA-Z0-9_./-]+/m/[a-zA-Z0-9_]+/?$");

        //User:
        public static Regex UserRegex = new Regex("(?:^|\\s|reddit.com)/u(?:ser)*/[a-zA-Z0-9_/-]+/?$");

        public static async void GotoComments(string url, INavigationContext context, LinkViewModel linkViewModel)
        {
            if (linkViewModel == null)
            {
                var linkContext = context.MakeLinkContext(url);
                //TODO: if this takes too long pop a loading dialog with cancel
                Progress<float> dummyProgress = new Progress<float>();
                linkViewModel = await LinkBuilder.MakeLinkViewModel(url, linkContext, CancellationToken.None, dummyProgress);

            }
            var commentsViewModel = context.MakeCommentContext(url, null, null, linkViewModel);
            context.HubNav.Navigate(commentsViewModel, context.CommentTemplate, false);
        }

        public static void GotoContentSettings(ISettingsContext settingsContext, INavigationContext context)
        {
            context.HubNav.Navigate(new ContentSettingsViewModel(settingsContext), context.ContentSettingsTemplate, false);
        }

        public static void GotoUserDetails(string username, INavigationContext context)
        {
            //check if we're looking at our self so we dont go to a generic about user page
            if (string.Compare(context.SelfViewModel.Username, username, false) == 0)
            {
                GotoSelf(context);
            }
            else
            {
                var userContext = context.MakeUserDetailsContext(username);
                context.HubNav.Navigate(userContext, context.UserDetailsTemplate, false);
            }
        }

        public static void GotoLink(object contextObj, string url, INavigationContext context)
        {
            if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
            {
                return;
            }

            Uri uri = new Uri(url);


            if (CommentRegex.IsMatch(url) ||
                CommentsPageRegex.IsMatch(url) ||
                ShortCommentsPageRegex.IsMatch(url) &&
                !uri.AbsolutePath.EndsWith(".jpg") &&
                !uri.AbsolutePath.EndsWith(".jpeg") &&
                !uri.AbsolutePath.EndsWith(".png") &&
                !uri.AbsolutePath.EndsWith(".gif"))
            {
                GotoComments(url, context, contextObj as LinkViewModel);
            }
            else if (SubredditRegex.IsMatch(url))
            {
                var nameIndex = url.LastIndexOf("/r/");
                var subredditName = url.Substring(nameIndex + 3);

                GotoSubreddit(subredditName, context);
            }
            else if (UserMultiredditRegex.IsMatch(url))
            {
                var nameIndex = url.LastIndexOf("/u/");
                string subredditName = "";
                if (nameIndex < 0)
                {
                    nameIndex = url.LastIndexOf("/user/");
                    subredditName = url.Substring(nameIndex);
                }
                else
                {
                    subredditName = url.Substring(nameIndex);
                }

                subredditName = subredditName.Replace("/u/", "/user/");

                GotoSubreddit(subredditName, context);
            }
            else if (UserRegex.IsMatch(url))
            {
                var nameIndex = url.LastIndexOf("/u/");
                string userName = "";
                if (nameIndex < 0)
                {
                    nameIndex = url.LastIndexOf("/user/");
                    userName = url.Substring(nameIndex + 6);
                }
                else
                {
                    userName = url.Substring(nameIndex + 3);
                }

                GotoUserDetails(userName, context);
            }
            //TODO this should be using a regex and be less bad
            else if (url.Contains("microsoft.com/") && url.Contains("/store/"))
            {
                context.LaunchUri(url);
            }
            else
            {
                GotoContentRiver(contextObj, url, context);
            }
        }

        public static ContentRiverViewModel GotoContentRiver(object contextObj, string url, INavigationContext context)
        {
            var contentContext = context.MakeContentRiverContext(contextObj, url);
            context.HubNav.Navigate(contentContext, context.ContentRiverTemplate, false);
            return contentContext;
        }

        public static SearchViewModel GotoSearch(string query, string restrictedToSubreddit, bool subredditsOnly, INavigationContext context)
        {
            var result = context.MakeSearchContext(query, restrictedToSubreddit, subredditsOnly);
            context.HubNav.Navigate(result, context.SearchTemplate, true);
            return result;
        }

        public static LoginViewModel GotoSelf(INavigationContext context)
        {
            context.HubNav.Navigate(context.SelfViewModel, context.SelfTemplate, true);
            return context.LoginViewModel;
        }

        public static LoginViewModel GotoLogin(INavigationContext context)
        {
            context.HubNav.Navigate(context.LoginViewModel, context.LoginTemplate, true);
            return context.LoginViewModel;
        }

        public static LinkRiverViewModel GotoSubreddit(string subreddit, INavigationContext context)
        {
            var result = context.MakeLinkRiverContext(subreddit, null, null);
            context.HubNav.Navigate(result, context.LinkRiverTemplate, true);
            return result;
        }

        private static T GetOrNull<T>(Dictionary<string, object> source, string key) where T : class
        {
            object result;
            if (source.TryGetValue(key, out result))
                return result as T;
            else
                return null;
        }

        public static void GotoStatePage(Dictionary<string, object> pageState, INavigationContext context)
        {
            var pageKind = pageState["kind"] as string;
            switch (pageKind)
            {
                case "subredditRiver":
                    context.HubNav.Navigate(context.SubredditRiver, context.SubredditRiverTemplate, true);
                    break;
                case "subreddit":
                    context.HubNav.Navigate(context.MakeLinkRiverContext(GetOrNull<string>(pageState, "url"), GetOrNull<string>(pageState, "focusId"), GetOrNull<string>(pageState, "sort")), context.LinkRiverTemplate, true);
                    break;
                case "search":
                    GotoSearch(GetOrNull<string>(pageState, "query"), GetOrNull<string>(pageState, "restrictedToSubreddit"), (bool)GetOrNull<object>(pageState, "subredditsOnly"), context);
                    break;
                case "self":
                    GotoSelf(context);
                    break;
                case "login":
                    GotoLogin(context);
                    break;
                case "content":
                    GotoContentRiver(context.ViewModelStack.LastOrDefault(vm => vm is LinkRiverViewModel || vm is CommentsViewModel), GetOrNull<string>(pageState, "url"), context);
                    break;
                case "comments":
                    context.HubNav.Navigate(context.MakeCommentContext(GetOrNull<string>(pageState, "url"), GetOrNull<string>(pageState, "focusId"), GetOrNull<string>(pageState, "sort"), null), context.CommentTemplate, false);
                    break;
                case "user":
                    GotoUserDetails(GetOrNull<string>(pageState, "username"), context);
                    break;
            }
        }

        public static Dictionary<string, object> MakeStatePage(object viewModel, INavigationContext context)
        {
            if (viewModel is LinkRiverViewModel)
                return context.MakePageState(((LinkRiverViewModel)viewModel));
            else if (viewModel is ContentRiverViewModel)
                return context.MakePageState(((ContentRiverViewModel)viewModel));
            else if (viewModel is CommentsViewModel)
                return context.MakePageState(((CommentsViewModel)viewModel));
            else if (viewModel is SearchViewModel)
                return context.MakePageState((SearchViewModel)viewModel);
            else if (viewModel is UserViewModel)
                return context.MakePageState((UserViewModel)viewModel);
            else if (viewModel is LoginViewModel)
                return new Dictionary<string, object> { { "kind", "login" } };
            else if (viewModel is SubredditRiverViewModel)
                return new Dictionary<string, object> { { "kind", "subredditRiver" } };
            else
                throw new InvalidOperationException("invalid view model while making state page");
        }
    }

    class NavigationContext : INavigationContext
    {
        CoreDispatcher _coreDispatcher;
        public Reddit Reddit { get; set; }
        public OfflineService Offline { get; set; }
        public PlainNetworkLayer NetworkLayer { get; set; }
        public UserState UserState { get; set; }
        public ActivityManager ActivityManager { get; set; }
        public ActivitiesViewModel ActivitiesViewModel { get; set; }
        public LoginViewModel LoginViewModel { get; set; }
        public SelfViewModel SelfViewModel { get; set; }
        public SettingsViewModel SettingsViewModel { get; set; }
        public SubredditRiverViewModel SubredditRiver { get; set; }
        public RoamingState RoamingState { get; set; }
        public NavMenu NavMenu { get; set; }
        public AdaptiveHubNav HubNav { get; set; }

        public DataTemplate LinkRiverTemplate { get { return _linkRiverTemplate.Value; } }
        public DataTemplate LoginTemplate { get { return _loginTemplate.Value; } }
        public DataTemplate ContentRiverTemplate { get { return _contentRiverTemplate.Value; } }
        public DataTemplate CommentTemplate { get { return _commentTemplate.Value; } }
        public DataTemplate SubredditRiverTemplate { get { return _subredditRiverTemplate.Value; } }
        public DataTemplate SearchTemplate { get { return _searchTemplate.Value; } }
        public DataTemplate SelfTemplate { get { return _selfTemplate.Value; } }
        public DataTemplate UserDetailsTemplate { get { return _userDetailsTemplate.Value; } }
        public DataTemplate SettingsTemplate { get { return _settingsTemplate.Value; } }
        public DataTemplate OAuthLandingTemplate { get { return _oAuthLandingTemplate.Value; } }
        public DataTemplate ContentSettingsTemplate { get { return _contentSettingsTemplate.Value; } }

        private LazyTemplate _linkRiverTemplate;
        private LazyTemplate _loginTemplate;
        private LazyTemplate _contentRiverTemplate;
        private LazyTemplate _commentTemplate;
        private LazyTemplate _subredditRiverTemplate;
        private LazyTemplate _searchTemplate;
        private LazyTemplate _selfTemplate;
        private LazyTemplate _userDetailsTemplate;
        private LazyTemplate _settingsTemplate;
        private LazyTemplate _oAuthLandingTemplate;
        private LazyTemplate _contentSettingsTemplate;


        public PeriodicTask PeriodicTasks;
        public IEnumerable<object> ViewModelStack
        {
            get
            {
                if (HubNav.NavStack != null)
                    return HubNav.NavStack.Select(item => item.Content);
                else
                    return Enumerable.Empty<object>();
            }
        }

        //this should include anything that changes infrequently and needs to exist
        static List<string> HighValueListingUrls = new List<string>
        {
            "https://oauth.reddit.com/subreddits/mine/subscriber",
            "https://oauth.reddit.com/subreddits/mine/moderator",
            "https://oauth.reddit.com/reddits/",
            "http://www.reddit.com/reddits/"
        };

        

        public NavigationContext(AdaptiveHubNav hubNav)
        {
            Debug.Assert(Window.Current != null && Window.Current.Dispatcher != null, "CoreDispatcher was null for current window");
            _coreDispatcher = Window.Current.Dispatcher;

            CommonResourceAcquisition.ImageAcquisition.ImageAcquisition.ImgurAPIKey = "cf771dfe25a7462";
            PeriodicTask.DefaultTask = PeriodicTasks = new PeriodicTask(120000);
            PeriodicTasks.Run();


            _subredditRiverTemplate = new LazyTemplate<SubredditRiverTemplate>("SubredditRiverView"); 
            _linkRiverTemplate = new LazyTemplate<LinkRiverTemplate>("LinkRiverView");
            _commentTemplate = new LazyTemplate<CommentsTemplate>("CommentsView");
            _contentRiverTemplate = new LazyTemplate<ContentRiverTemplate>("ContentRiverView");
            _searchTemplate = new LazyTemplate<SearchViewTemplate>("SearchView");
            _userDetailsTemplate = new LazyTemplate<UserDetailsTemplate>("UserDetails");
            _selfTemplate = new LazyTemplate<SelfActivityTemplate>("SelfView");
            _loginTemplate = new LazyTemplate<LoginTemplate>("LoginView");
            _oAuthLandingTemplate = new LazyTemplate<LoginTemplate>("OAuthLandingView");
            _settingsTemplate = new LazyTemplate<SettingsTemplate>("SettingsView");
            _contentSettingsTemplate = new LazyTemplate<SettingsTemplate>("ContentSettingsView");


            HubNav = hubNav;
            RoamingState = new RoamingState();
            var settingsContext = new SettingsContext { Settings = RoamingState.Settings ?? new Dictionary<string, string>(), Navigation = this };
            SettingsViewModel = new SettingsViewModel(settingsContext);
            ActivityManager = new ActivityManager();
            Offline = new OfflineService();
            NetworkLayer = new PlainNetworkLayer();
            UserState = RoamingState.UserCredentials?.FirstOrDefault(state => state.IsDefault);
            var listingFilterContext = new ListingFilterContext { Offline = Offline, Settings = SettingsViewModel };
            var cacheProvider = new SnooSharpCacheProvider(Offline, HighValueListingUrls);
            Reddit = new Reddit(new NSFWListingFilter(listingFilterContext),
               UserState,
                Offline,
                new CaptchaService(),
                "3m9rQtBinOg_rA", 
                null, 
                "http://www.google.com",
                cacheProvider, 
                new SnooSharpNetworkLayer(UserState, "3m9rQtBinOg_rA", null, "http://www.google.com"));

            listingFilterContext.Reddit = Reddit;

            var loginContext = new LoginContext { Reddit = Reddit, RoamingState = RoamingState, Navigation = this };
            ActivitiesViewModel = new ActivitiesViewModel(new ActivityBuilderContext(Reddit, ActivityManager, loginContext));
            var selfContext = new SelfContext();
            SelfViewModel = new SelfViewModel(selfContext);
            LoginViewModel = new LoginViewModel(loginContext);
            SubredditRiver = new SubredditRiverViewModel(new SubredditRiverContext { Reddit = Reddit, NavigationContext = this });
            NavMenu = new NavMenu(new NavMenuContext { Reddit = Reddit, ActivityManager = ActivityManager },
                LoginViewModel, SelfViewModel, ActivitiesViewModel, 
                SettingsViewModel, MakeSearchContext("", "", false), SubredditRiver);

            ((VisitedLinkConverter)Application.Current.Resources["visitedLinkConverter"]).Offline = Offline;
            ((VisitedMainLinkConverter)Application.Current.Resources["visitedMainLinkConverter"]).Offline = Offline;
            ((MarkdownHelpers)Application.Current.Resources["markdownHelpers"]).NavigationContext = this;
            ((LinkMetadataConverter)Application.Current.Resources["linkMetadataConverter"]).NavigationContext = this;
            ((LinkViewLayoutManager)Application.Current.Resources["linkViewLayoutManager"]).Settings = settingsContext;

            var navStack = RoamingState.NavStack?.ToList() ?? new List<Dictionary<string, object>>();
            if (navStack.Count > 0)
            {
                foreach (var statePage in navStack)
                {
                    Navigation.GotoStatePage(statePage, this);
                }
            }
            else
            {
                Navigate(SubredditRiver);
            }
        }


        public async void LaunchUri(string uri)
        {
            try
            {
                await Launcher.LaunchUriAsync(new Uri(uri));
            }
            catch (Exception)
            {
                //TODO message box this?
            }
        }

        public void Navigate(object viewModel)
        {
            HubNav.Navigate(viewModel, MapVMToPageType(viewModel), false);
        }

        DataTemplate MapVMToPageType(object vm)
        {
            if (vm is LoginViewModel)
                return LoginTemplate;
            else if (vm is ActivitiesViewModel)
                return SelfTemplate;
            else if (vm is UserViewModel)
                return UserDetailsTemplate;
            else if (vm is LinkRiverViewModel)
                return LinkRiverTemplate;
            else if (vm is SettingsViewModel)
                return SettingsTemplate;
            else if (vm is SearchViewModel)
                return SearchTemplate;
            else if (vm is SubredditRiverViewModel)
                return SubredditRiverTemplate;
            else if (vm is SettingsViewModel)
                return SettingsTemplate;
            else if (vm is ContentSettingsViewModel)
                return ContentSettingsTemplate;
            //else
            return null;
        }

        public CommentsViewModel MakeCommentContext(string url, string focusId, string sort, LinkViewModel linkViewModel)
        {
            var madeContext = new CommentBuilderContext { Reddit = Reddit, PermaLink = linkViewModel?.Thing?.Permalink, Url = url, Sort = sort ?? "best", ContextTarget = focusId, NavigationContext = this, Dispatcher = _coreDispatcher };
            var madeComments = new CommentsViewModel(madeContext);
            madeContext.Comments = madeComments;
            var targetLink = linkViewModel ?? new LinkViewModel { Thing = new Link(), Context = new CommentLinkContext { NavigationContext = this, Reddit = Reddit, ViewModel = madeComments }, Votable = new VotableViewModel(new Link(), madeContext.ChangeVote) };
            madeContext.Link = targetLink;

            targetLink.SelfText = SnooDom.SnooDom.MarkdownToDOM(targetLink.Thing.Selftext ?? string.Empty, madeContext._markdownMemoryPool);

            //load needs to happen after everything is constructed but before we return otherwise things might be partially setup if we have cached data (or go too fast)
            madeComments.Load(); 
            return madeComments;
        }

        public ContentRiverViewModel MakeContentRiverContext(object context, string url)
        {
            if (context is LinkRiverViewModel)
            {
                var madeContext = new LinkContentRiverContext { LinkRiverContext = context as LinkRiverViewModel, NetworkLayer = NetworkLayer, NavigationContext = this };
                var madeContentRiver = new ContentRiverViewModel(madeContext);
                madeContext.Collection = madeContentRiver.ContentItems;
                madeContext.ContentView = madeContentRiver.ContentItems;

                foreach (var item in madeContext.LoadInitial(url))
                {
                    madeContentRiver.ContentItems.Add(item);
                }
                madeContentRiver.ContentItems.CurrentPosition = madeContext.Current;

                return madeContentRiver;
            }
            else if (context is CommentsViewModel)
            {
                var madeContext = new CommentContentRiverContext { InitialUrl = url, Comments = context as CommentsViewModel, NetworkLayer = NetworkLayer, NavigationContext = this };
                var madeContentRiver = new ContentRiverViewModel(madeContext);
                madeContext.Collection = madeContentRiver.ContentItems;
                madeContext.CollectionView = madeContentRiver.ContentItems;

                madeContentRiver.ContentItems.LoadMoreItemsAsync(50).AsTask().ContinueWith(result => madeContentRiver.ContentItems.CurrentPosition = madeContext.Current);
                return madeContentRiver;
            }
            else
                throw new InvalidOperationException("unknown context type");
           
        }

        public LinkRiverViewModel MakeLinkRiverContext(string subreddit, string focusId, string sort)
        {
            var linkContext = new LinkContext { NavigationContext = this, Reddit = Reddit };
            var madeContext = new LinkBuilderContext { Offline = Offline, Reddit = Reddit, Subreddit = Utility.CleanRedditLink(subreddit, this.Reddit.CurrentUserName), LinkContext = linkContext, NavigationContext = this };
            var madeViewModel = new LinkRiverViewModel(madeContext);
            linkContext.LinkRiver = madeViewModel;
            return madeViewModel;
        }

        public SearchViewModel MakeSearchContext(string query, string restrictedToSubreddit, bool subredditsOnly)
        {
            var linkContext = new SearchLinkContext { NavigationContext = this, Reddit = Reddit };
            var searchContext = new SearchContext(query, restrictedToSubreddit, subredditsOnly, Reddit, this, Offline, linkContext, _coreDispatcher);
            var madeViewModel = new SearchViewModel(searchContext);
            linkContext.SearchViewModel = madeViewModel;
            return madeViewModel;
        }

        public UserViewModel MakeUserDetailsContext(string username)
        {
            var linkContext = new UserLinkContext { NavigationContext = this, Reddit = Reddit };
            var userContext = new UserContext(username, Reddit, this, Offline, linkContext);
            var madeViewModel = new UserViewModel(userContext);
            return madeViewModel;
        }

        public async Task SaveState()
        {
            var hubNavItems = this.HubNav.NavStack;
            RoamingState.NavStack = hubNavItems.Select(hubNavItem => Navigation.MakeStatePage(hubNavItem.Content, this)).ToList();
            await PeriodicTasks.Suspend();
        }

        public Dictionary<string, object> MakePageState(SettingsViewModel settings)
        {
            return new Dictionary<string, object>
            {
                { "kind", "settings" }
            };
        }

        public Dictionary<string, object> MakePageState(ContentSettingsViewModel settings)
        {
            return new Dictionary<string, object>
            {
                { "kind", "contentSettings" }
            };
        }

        public Dictionary<string, object> MakePageState(CommentsViewModel comment)
        {
            var typedContext = comment.Context as CommentBuilderContext;
            return new Dictionary<string, object>
            {
                { "kind", "comments" },
                { "url", typedContext.Url },
                { "focusId", GetIdFromCommentItem(comment.Comments.CurrentItem) },
                { "sort", typedContext.Sort }
            };
        }

        public Dictionary<string, object> MakePageState(UserViewModel user)
        {
            var typedContext = user.Context as UserContext;
            return new Dictionary<string, object>
            {
                { "kind", "user" },
                { "username", typedContext.TargetUser }
            };
        }

        private string GetIdFromCommentItem(object commentItem)
        {
            if (commentItem is CommentViewModel)
            {
                return ((CommentViewModel)commentItem).Thing.Name;
            }
            else if (commentItem is MoreViewModel)
            {
                return ((MoreViewModel)commentItem).Ids.FirstOrDefault();
            }
            else if (commentItem is LoadFullCommentsViewModel || commentItem is LoadViewModel)
            {
                return null;
            }
            else if (commentItem == null)
            {
                return null;
            }
            else
            {
                throw new InvalidOperationException("attempted to get id from comment item with invalid type");
            }
        }

        public Dictionary<string, object> MakePageState(LinkRiverViewModel linkRiver)
        {
            var typedContext = linkRiver.Context as LinkBuilderContext;
            var state = new Dictionary<string, object>
            {
                { "kind", "subreddit" },
                { "url", typedContext.Subreddit },
                { "sort", typedContext.Sort }
            };

            if (linkRiver.Links.CurrentItem != null)
            {
                state.Add("focusId", GetIdFromLinkItem(linkRiver.Links.CurrentItem, linkRiver.LastLinkId));
            }
            return state;
        }

        private string GetIdFromLinkItem(object linkItem, string lastLinkId)
        {
            if (linkItem is LinkViewModel)
            {
                return ((LinkViewModel)linkItem).Thing.Id;
            }
            else if (linkItem is LoadViewModel)
            {
                return lastLinkId;
            }
            else
            {
                throw new InvalidOperationException("invalid link item type when trying to get id");
            }
        }

        public Dictionary<string, object> MakePageState(ContentRiverViewModel contentRiver)
        {
            return new Dictionary<string, object>
            {
                { "kind", "content" },
                { "url", GetUrlFromContentItem(contentRiver.ContentItems.CurrentItem, contentRiver.ContentItems.LastOrDefault()) },
            };
        }

        public string GetUrlFromContentItem(object contentItem, object lastItem)
        {
            if (contentItem is ContentViewModel)
            {
                return ((ContentViewModel)contentItem).Url;
            }
            else if (lastItem != null)
            {
                return GetUrlFromContentItem(lastItem, null);
            }
            else
                return null;
        }

        public ILinkContext MakeLinkContext(string url)
        {
            return new LinkContext { NavigationContext = this, Reddit = Reddit };
        }

        public Dictionary<string, object> MakePageState(SearchViewModel search)
        {
            return new Dictionary<string, object>
            {
                { "kind", "search" },
                { "query", search.Query },
                { "subredditsOnly", search.SubredditsOnly },
                { "restrictedToSubreddit", search.RestrictedToSubreddit }
            };
        }

        public Dictionary<string, object> MakePageState(LoginViewModel login)
        {
            return new Dictionary<string, object>
            {
                { "kind", "login" }
            };
        }

        public Dictionary<string, object> MakePageState(SelfViewModel self)
        {
            return new Dictionary<string, object>
            {
                { "kind", "self" },
            };
        }

        
    }
}
