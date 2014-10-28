using CommonResourceAcquisition.ImageAcquisition;
using CommonResourceAcquisition.VideoAcquisition;
using GalaSoft.MvvmLight;
using SnooSharp;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public class CommandDispatcher
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

        public void GotoCommentContext(ViewModelBase currentContext, CommentViewModel source)
        {
        }

        public void GotoFullComments(ViewModelBase currentContext, CommentViewModel source)
        {

        }

        public void GotoUserDetails(string username)
        {
            throw new NotImplementedException();
        }

        public void GotoReplyToComment(ViewModelBase currentContext, CommentViewModel source)
        {
            throw new NotImplementedException();
        }

        public void GotoReplyToPost(ViewModelBase currentContext, CommentsViewModel source)
        {
            throw new NotImplementedException();
        }

        public void GotoEditComment(ViewModelBase currentContext, CommentViewModel source)
        {
            throw new NotImplementedException();
        }

        public void GotoEditPost(ViewModelBase currentContext, LinkViewModel source)
        {
            throw new NotImplementedException();
        }

        public void UpdateComment(ViewModelBase _context, Thing theComment)
        {
            throw new NotImplementedException();
        }

        public void InsertComment(ViewModelBase _context, Thing theComment)
        {
            throw new NotImplementedException();
        }

        public async void GotoLink(object contextObj, string url)
        {
			var context = contextObj as ViewModelBase;
            if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
            {
                return;
            }

            var task = SnooStreamViewModel.NotificationService.ModalReportWithCancelation(string.Format("navigating to {0}", url), async (token) =>
            {
                var sourceLink = context is LinkViewModel ? ((LinkViewModel)context).Link : null;
                var linkContext = context as LinkViewModel;
                var riverContext = linkContext != null ? linkContext.Context : null;

				if (context is LinkViewModel)
				{
					var linkRiver = ((LinkViewModel)context).Context as LinkRiverViewModel;
					if(linkRiver != null)
						linkRiver.CurrentSelected = context as ILinkViewModel;
				}

                if (CommentRegex.IsMatch(url))
                {
                    var targetLinkThing = sourceLink == null ? await SnooStreamViewModel.RedditService.GetLinkByUrl(url) :
                        new Thing { Kind = "t3", Data = new Link { Permalink = url, Url = url, Title = url, Name = "", Author = "", Selftext = "" } };

                    token.ThrowIfCancellationRequested();

                    if (targetLinkThing != null && targetLinkThing.Data is Link)
                        SnooStreamViewModel.NavigationService.NavigateToComments(new CommentsViewModel(context, targetLinkThing.Data as Link));
                    else
                    {
                        SnooStreamViewModel.NavigationService.NavigateToWeb(url);
                    }
                }
                else if (CommentsPageRegex.IsMatch(url))
                {
                    if (sourceLink != null)
                    {
                        var targetLinkPermalink = sourceLink.Url.Substring(sourceLink.Url.IndexOf("reddit.com") + "reddit.com".Length);
                        if (!targetLinkPermalink.Contains(url))
                        {
                            var realTarget = await SnooStreamViewModel.RedditService.GetLinkByUrl(url);
                            if (realTarget != null)
                            {
                                sourceLink = realTarget.Data as Link;
                            }
                        }
                    }

                    var targetLinkThing = sourceLink == null ? await SnooStreamViewModel.RedditService.GetLinkByUrl(url) : new Thing { Kind = "t3", Data = sourceLink };

                    token.ThrowIfCancellationRequested();

                    if (targetLinkThing != null)
                    {
                        SnooStreamViewModel.OfflineService.AddHistory(((Link)targetLinkThing.Data).Permalink);
                        SnooStreamViewModel.NavigationService.NavigateToComments(new CommentsViewModel(context, targetLinkThing.Data as Link));
                    }
                    else
                    {
                        SnooStreamViewModel.NavigationService.NavigateToWeb(url);
                    }
                }
                else if (ShortCommentsPageRegex.IsMatch(url))
                {
                    var thingId = "t3_" + url.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Last();
                    var targetLinkThing = sourceLink == null ? await SnooStreamViewModel.RedditService.GetThingById(thingId) : new Thing { Kind = "t3", Data = sourceLink };

                    token.ThrowIfCancellationRequested();

                    if (targetLinkThing != null)
                    {
                        SnooStreamViewModel.OfflineService.AddHistory(((Link)targetLinkThing.Data).Permalink);
                        SnooStreamViewModel.NavigationService.NavigateToComments(new CommentsViewModel(context, targetLinkThing.Data as Link));
                    }
                    else
                    {
                        SnooStreamViewModel.NavigationService.NavigateToWeb(url);
                    }
                }
                else if (SubredditRegex.IsMatch(url))
                {
                    var nameIndex = url.LastIndexOf("/r/");
                    var subredditName = url.Substring(nameIndex + 3);

                    TypedThing<Subreddit> subreddit = null;

                    if (SnooStreamViewModel.SystemServices.IsHighPriorityNetworkOk)
                    {
                        subreddit = await SnooStreamViewModel.RedditService.GetSubreddit(subredditName);
                    }
                    else
                    {
                        var thing = await SnooStreamViewModel.OfflineService.GetSubreddit(subredditName);
                        if (thing != null)
                            subreddit = new TypedThing<Subreddit>(thing);
                    }

                    token.ThrowIfCancellationRequested();

                    if (subreddit != null)
                        SnooStreamViewModel.NavigationService.NavigateToLinkRiver(new LinkRiverViewModel(false, subreddit.Data, null, null, null));
                    else
                        throw new RedditException("subreddit unavailable");
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

                    TypedThing<Subreddit> subreddit = null;

                    if (SnooStreamViewModel.SystemServices.IsHighPriorityNetworkOk)
                    {
                        subreddit = await SnooStreamViewModel.RedditService.GetSubreddit(subredditName);
                    }
                    else
                    {
                        var thing = await SnooStreamViewModel.OfflineService.GetSubreddit(subredditName);
                        if (thing != null)
                            subreddit = new TypedThing<Subreddit>(thing);
                    }

                    token.ThrowIfCancellationRequested();

                    if (subreddit != null)
                        SnooStreamViewModel.NavigationService.NavigateToLinkRiver(new LinkRiverViewModel(false, subreddit.Data, null, null, null));
                    else
                        throw new RedditException("subreddit unavailable");
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

                    TypedThing<Account> account = null;

                    if (SnooStreamViewModel.SystemServices.IsHighPriorityNetworkOk)
                    {
                        account = await SnooStreamViewModel.RedditService.GetAccountInfo(userName);

                        token.ThrowIfCancellationRequested();

                        if (account != null)
                            SnooStreamViewModel.NavigationService.NavigateToAboutUser(new AboutUserViewModel(account));
                        else
                            throw new RedditException("userinfo doesnt exist");
                    }
                    else
                    {
                        throw new RedditException("userinfo unavailable");
                    }
                }
                else
                {
					if (context is LinkViewModel)
					{
						var linkRiver = ((LinkViewModel)context).Context as LinkRiverViewModel;
						SnooStreamViewModel.NavigationService.NavigateToContentRiver(linkRiver);
					}
					else if (contextObj is Tuple<CommentsViewModel, CommentViewModel, string>)
					{
						var contextTpl = contextObj as Tuple<CommentsViewModel, CommentViewModel, string>;
						var contentStream = contextTpl.Item1.CommentsContentStream;
						var targetContent = contentStream.Links.FirstOrDefault(item => item.Id == contextTpl.Item2.Id && item.Url == contextTpl.Item3);
						if (targetContent != null)
							contentStream.CurrentSelected = context as ILinkViewModel;

						SnooStreamViewModel.NavigationService.NavigateToContentRiver(contentStream);
					}
                }
            });
            await task;
        }

        public void GotoLogin(ViewModelBase vm)
        {
            throw new NotImplementedException();
        }

        public async void GotoSubreddit(string subreddit)
        {
            await SnooStreamViewModel.NotificationService.ModalReportWithCancelation(string.Format("navigating to {0}", subreddit), async (token) =>
            {
                TypedThing<Subreddit> subredditThing = null;
                if (SnooStreamViewModel.SystemServices.IsHighPriorityNetworkOk)
                {
                    subredditThing = await SnooStreamViewModel.RedditService.GetSubreddit(subreddit);
                }
                else
                {
                    var thing = await SnooStreamViewModel.OfflineService.GetSubreddit(subreddit);
                    if (thing != null)
                        subredditThing = new TypedThing<Subreddit>(thing);
                }

                token.ThrowIfCancellationRequested();

                if (subredditThing != null)
                    SnooStreamViewModel.NavigationService.NavigateToLinkRiver(new LinkRiverViewModel(false, subredditThing.Data, null, null, null));
                else
                    throw new RedditException("subreddit unavailable");
            });
        }
    }
}
