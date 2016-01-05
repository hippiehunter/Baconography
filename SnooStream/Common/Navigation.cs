﻿using SnooSharp;
using SnooStream.Controls;
using SnooStream.Converters;
using SnooStream.Model;
using SnooStream.Templates;
using SnooStream.ViewModel;
using SnooStreamBackground;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SnooStream.Common
{
    public interface INavigationContext
    {
        void LaunchUri(string uri);
        void Navigate(object viewModel);
        void SaveNavigationState();
        ContentRiverViewModel MakeContentRiverContext(object context, string url);
        CommentsViewModel MakeCommentContext(string url, string focusId, string sort, LinkViewModel linkViewModel);
        LinkRiverViewModel MakeLinkRiverContext(string subreddit, string focusId, string sort);

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

        IEnumerable<object> ViewModelStack { get; }

        Dictionary<string, object> MakePageState(CommentsViewModel comments);
        Dictionary<string, object> MakePageState(LinkRiverViewModel links);
        Dictionary<string, object> MakePageState(ContentRiverViewModel contentRiver);
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
                linkViewModel = await LinkBuilder.MakeLinkViewModel(url, linkContext);

            }
            var commentsViewModel = context.MakeCommentContext(url, null, null, linkViewModel);
            context.HubNav.Navigate(commentsViewModel, context.CommentTemplate, false);
        }

        public static void GotoUserDetails(string username, INavigationContext context)
        {
            //check if we're looking at our self so we dont go to a generic about user page
            //SnooStreamViewModel.NavigationService.NavigateToAboutUser(new AboutUserViewModel(username));
        }

        public static void GotoLink(object contextObj, string url, INavigationContext context)
        {
            if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
            {
                return;
            }

            if (CommentRegex.IsMatch(url) || 
                CommentsPageRegex.IsMatch(url) ||
                ShortCommentsPageRegex.IsMatch(url))
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

        public static void GotoStatePage(Dictionary<string, object> pageState, INavigationContext context)
        {
            var pageKind = pageState["kind"] as string;
            switch (pageKind)
            {
                case "subredditRiver":
                    context.HubNav.Navigate(context.SubredditRiver, context.SubredditRiverTemplate, true);
                    break;
                case "subreddit":
                    context.HubNav.Navigate(context.MakeLinkRiverContext(pageState["subreddit"] as string, pageState["focusId"] as string, pageState["sort"] as string), context.LinkRiverTemplate, true);
                    break;
                case "login":
                    GotoLogin(context);
                    break;
                case "content":
                    GotoContentRiver(context.ViewModelStack.LastOrDefault(vm => vm is LinkRiverViewModel || vm is CommentsViewModel), pageState["url"] as string, context);
                    break;
                case "comments":
                    context.HubNav.Navigate(context.MakeCommentContext(pageState["url"] as string, pageState["focusId"] as string, pageState["sort"] as string, null), context.CommentTemplate, false);
                    break;
                case "user":
                    GotoUserDetails(pageState["username"] as string, context);
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
        public Reddit Reddit { get; set; }
        public OfflineService Offline { get; set; }
        public PlainNetworkLayer NetworkLayer { get; set; }

        public ActivityManager ActivityManager { get; set; }
        public ActivitiesViewModel ActivitiesViewModel { get; set; }
        public LoginViewModel LoginViewModel { get; set; }
        public SelfViewModel SelfViewModel { get; set; }
        public SettingsViewModel SettingsViewModel { get; set; }
        public SubredditRiverViewModel SubredditRiver { get; set; }
        public RoamingState RoamingState { get; set; }
        public NavMenu NavMenu { get; set; }
        public AdaptiveHubNav HubNav { get; set; }

        public DataTemplate LinkRiverTemplate { get; set; }
        public DataTemplate LoginTemplate { get; set; }
        public DataTemplate ContentRiverTemplate { get; set; }
        public DataTemplate CommentTemplate { get; set; }
        public DataTemplate SubredditRiverTemplate { get; set; }
        public List<ResourceDictionary> ResourceDictionaryHandles { get; set; } = new List<ResourceDictionary>();
        public IEnumerable<object> ViewModelStack
        {
            get
            {
                if (HubNav.DataContext is IEnumerable<HubNavItem>)
                    return ((IEnumerable<HubNavItem>)HubNav.DataContext).Select(item => item.Content);
                else
                    return Enumerable.Empty<object>();
            }
        }

        public NavigationContext(AdaptiveHubNav hubNav)
        {
            var subredditRiverRD = new SubredditRiverTemplate();
            var linkRiverRD = new LinkRiverTemplate();
            var commentsRD = new CommentsTemplate();
            var contentRiverRD = new ContentRiverTemplate();
            ResourceDictionaryHandles.Add(commentsRD);
            ResourceDictionaryHandles.Add(subredditRiverRD);
            ResourceDictionaryHandles.Add(linkRiverRD);
            ResourceDictionaryHandles.Add(contentRiverRD);
            SubredditRiverTemplate = subredditRiverRD["SubredditRiverView"] as DataTemplate;
            LinkRiverTemplate = linkRiverRD["LinkRiverView"] as DataTemplate;
            CommentTemplate = commentsRD["CommentsView"] as DataTemplate;
            ContentRiverTemplate = contentRiverRD["ContentRiverView"] as DataTemplate;

            HubNav = hubNav;
            RoamingState = new RoamingState();
            var settingsContext = new SettingsContext { Settings = RoamingState.Settings ?? new Dictionary<string, string>() };
            SettingsViewModel = new SettingsViewModel(settingsContext);
            ActivityManager = new ActivityManager();
            Offline = new OfflineService();
            NetworkLayer = new PlainNetworkLayer();
            var userState = RoamingState.UserCredentials?.FirstOrDefault(state => state.IsDefault);
            var listingFilterContext = new ListingFilterContext { Offline = Offline, SettingsAllowOver18 = SettingsViewModel.AllowOver18, SettingsAllowOver18Items = SettingsViewModel.AllowOver18Items };
            Reddit = new Reddit(new NSFWListingFilter(listingFilterContext),
               userState,
                Offline,
                new CaptchaService(),
                "3m9rQtBinOg_rA", 
                null, 
                "http://www.google.com",
                new SnooSharpCacheProvider(), 
                new SnooSharpNetworkLayer(userState, "3m9rQtBinOg_rA", null, "http://www.google.com"));

            listingFilterContext.Reddit = Reddit;

            ActivitiesViewModel = new ActivitiesViewModel(new ActivityBuilderContext { Reddit = Reddit, ActivityManager = ActivityManager });
            var loginContext = new LoginContext { Reddit = Reddit, RoamingState = RoamingState };
            var selfContext = new SelfContext();
            SelfViewModel = new SelfViewModel(selfContext);
            LoginViewModel = new LoginViewModel(loginContext);
            SubredditRiver = new SubredditRiverViewModel(new SubredditRiverContext { Reddit = Reddit, NavigationContext = this });
            NavMenu = new NavMenu(new NavMenuContext { Reddit = Reddit, ActivityManager = ActivityManager },
                LoginViewModel, SelfViewModel, ActivitiesViewModel, 
                SettingsViewModel, new SearchViewModel(new SearchContext()), SubredditRiver);

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
            //if (vm is LoginViewModel)
            //    return typeof(LoginPage);
            //else if (vm is SelfStreamViewModel)
            //    return typeof(SelfActivityPage);
            //else if (vm is AboutUserViewModel)
            //    return typeof(SelfPage);
            if (vm is LinkRiverViewModel)
                return LinkRiverTemplate;
            //else if (vm is SettingsViewModel)
            //    return typeof(SettingsPage);
            //else if (vm is SearchViewModel)
            //    return typeof(SearchPage);
            else if (vm is SubredditRiverViewModel)
                return SubredditRiverTemplate;
            //else
            return null;
        }

        public CommentsViewModel MakeCommentContext(string url, string focusId, string sort, LinkViewModel linkViewModel)
        {
            var madeContext = new CommentBuilderContext { Reddit = Reddit, Url = url, Sort = sort ?? "best", ContextTarget = focusId, Link = linkViewModel, NavigationContext = this };
            var madeComments = new CommentsViewModel(madeContext);
            madeContext.Comments = madeComments;
            return madeComments;
        }

        public ContentRiverViewModel MakeContentRiverContext(object context, string url)
        {
            if (context is LinkRiverViewModel)
            {
                var madeContext = new LinkContentRiverContext { LinkRiverContext = context as LinkRiverViewModel, NetworkLayer = NetworkLayer, NavigationContext = this };
                var madeContentRiver = new ContentRiverViewModel(madeContext);
                madeContext.Collection = madeContentRiver.ContentItems;
                madeContext.ContentView = madeContentRiver.ViewSource.View;

                foreach (var item in madeContext.LoadInitial())
                {
                    madeContentRiver.ContentItems.Add(item);
                }

                return madeContentRiver;
            }
            else if (context is CommentsViewModel)
            {
                var madeContext = new CommentContentRiverContext { Comments = context as CommentsViewModel, NetworkLayer = NetworkLayer };
                var madeContentRiver = new ContentRiverViewModel(madeContext);
                madeContext.Collection = madeContentRiver.ContentItems;
                madeContext.CollectionView = madeContentRiver.ViewSource.View;
                return madeContentRiver;
            }
            else
                throw new InvalidOperationException("unknown context type");
           
        }

        public LinkRiverViewModel MakeLinkRiverContext(string subreddit, string focusId, string sort)
        {
            var linkContext = new LinkContext { NavigationContext = this, Reddit = Reddit };
            var madeContext = new LinkBuilderContext { Offline = Offline, Reddit = Reddit, Subreddit = subreddit, LinkContext = linkContext };
            var madeViewModel = new LinkRiverViewModel(madeContext);
            linkContext.LinkRiver = madeViewModel;
            return madeViewModel;
        }

        public void SaveNavigationState()
        {
            var hubNavItems = this.HubNav.NavStack;
            RoamingState.NavStack = hubNavItems.Select(hubNavItem => Navigation.MakeStatePage(hubNavItem.Content, this)).ToList();
        }

        public Dictionary<string, object> MakePageState(CommentsViewModel comment)
        {
            var typedContext = comment.Context as CommentBuilderContext;
            return new Dictionary<string, object>
            {
                { "kind", "comments" },
                { "url", typedContext.Url },
                { "focusId", GetIdFromCommentItem(comment.CommentsViewSource.View.CurrentItem) },
                { "sort", typedContext.Sort }
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
            else
            {
                throw new InvalidOperationException("attempted to get id from comment item with invalid type");
            }
        }

        public Dictionary<string, object> MakePageState(LinkRiverViewModel linkRiver)
        {
            var typedContext = linkRiver.Context as LinkBuilderContext;
            return new Dictionary<string, object>
            {
                { "kind", "subreddit" },
                { "url", typedContext.Subreddit },
                { "focusId", GetIdFromLinkItem(linkRiver.LinkViewSource.View.CurrentItem, linkRiver.LastLinkId) },
                { "sort", typedContext.Sort }
            };
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
                { "url", GetUrlFromContentItem(contentRiver.ViewSource.View.CurrentItem, contentRiver.ViewSource.View.LastOrDefault()) },
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
    }
}
