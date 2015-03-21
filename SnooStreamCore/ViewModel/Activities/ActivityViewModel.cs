using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using NBoilerpipePortable.Util;
using SnooSharp;
using SnooStream.ViewModel.Popups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public abstract class ActivityViewModel : ViewModelBase
    {
        public class ActivityAgeComparitor : IComparer<ActivityViewModel>
        {
            public int Compare(ActivityViewModel x, ActivityViewModel y)
            {
                if (x is PostedLinkActivityViewModel)
                    return 1;
                else if (y is PostedLinkActivityViewModel)
                    return -1;
                else
                {
                    //invert the sort
                    var result = y.CreatedUTC.CompareTo(x.CreatedUTC);
                    if (result == 0 && ((ThingData)y.GetThing().Data).Id != ((ThingData)x.GetThing().Data).Id)
                        return 1;
                    else
                        return result;
                }
            }
        }

        public static Tuple<string, bool> CleanAuthor(string author)
        {
            if (string.Compare(author, SnooStreamViewModel.RedditUserState.Username, StringComparison.CurrentCultureIgnoreCase) == 0)
                return Tuple.Create("Me", true);
            else
                return Tuple.Create(author, false);
        }

        public abstract Thing GetThing();
        public DateTime CreatedUTC { get; protected set; }
		public bool IsSelf { get; set; }
        public string Title { get; set; }
		public string SubTitle { get; set; }
		public string PreviewBody { get; set; }
		internal static string Elipsis(string text, int maxLength)
		{
			if(text.Length > maxLength)
			{
				return text.Remove(maxLength - 3) + "...";
			}
			else
				return text;
		}

        public static async Task<Tuple<Listing, Thing>> FindTargetLink(string contextUrl, string linkId, string commentId, string commentName)
        {
            var contextListing = SnooStreamViewModel.ActivityManager.ContextForId(commentName);
            if (contextListing.Data.Children.Count == 0 && !string.IsNullOrWhiteSpace(contextUrl))
            {
                var splitUrl = contextUrl.Split('/');
                var subreddit = splitUrl[Array.IndexOf(splitUrl, "r") + 1];
                contextListing = await SnooStreamViewModel.RedditService.GetCommentsOnPost(subreddit, contextUrl, null);
            }
            Thing targetLinkThing = contextListing != null ? contextListing.Data.Children.FirstOrDefault(thing => thing.Data is Link) : null;
            if(targetLinkThing == null && !string.IsNullOrWhiteSpace(linkId))
            {
                targetLinkThing = await SnooStreamViewModel.RedditService.GetThingById(linkId);
            }


            if (contextListing == null && targetLinkThing != null && targetLinkThing.Data is Link)
            {
                var link = targetLinkThing.Data as Link;
                contextListing = await SnooStreamViewModel.RedditService.GetCommentsOnPost(link.Subreddit, link.Permalink + commentId + "?context=3", null);
            }

            return Tuple.Create(contextListing, targetLinkThing); 
        }

        public static async void NavigateToCommentContext(string contextUrl, string commentName, string commentId, string linkId)
        {
            await SnooStreamViewModel.NotificationService.ModalReportWithCancelation("navigating to context", async (token) =>
            {
                var targetInfo = await FindTargetLink(contextUrl, linkId, commentId, commentName);
                if (targetInfo.Item1 == null)
                {
                    throw new ArgumentException("unable to get context for contextUrl: " + contextUrl + " or linkId: " + linkId);
                }
                var linkViewModel = new LinkViewModel(null, targetInfo.Item2 != null ? targetInfo.Item2.Data as Link : null);
                var commentsViewModel = new CommentsViewModel(linkViewModel, targetInfo.Item1, contextUrl, null, true);

                SnooStreamViewModel.NavigationService.NavigateToComments(commentsViewModel);
            });
        }

        private static string StripCommonPrefix(string subject)
        {
            if (subject.ToLower().StartsWith("re:"))
                return subject.Substring(3).Trim();
            else
                return subject.Trim();
        }

        public static string GetActivityGroupName(Thing thing)
        {
            if (thing == null)
                throw new ArgumentNullException();

            if (thing.Data is Link)
                return ((Link)thing.Data).Name;
            else if (thing.Data is Comment)
            {
                if(((Comment)thing.Data).LinkId != null)
                    return ((Comment)thing.Data).LinkId;
                else
                    return ((Comment)thing.Data).ParentId;
            } 
            else if (thing.Data is Message)
            {
                var messageThing = thing.Data as Message;
                if (messageThing.WasComment)
                {
                    // "/r/{subreddit}/comments/{linkname}/{linktitleish}/{thingname}?context=3"

                    var splitContext = messageThing.Context.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    return "t3_" + splitContext[3];
                }
                else
                {
					return string.IsNullOrWhiteSpace(messageThing.FirstMessageName) ? messageThing.Name : messageThing.FirstMessageName;
                }
            }
            else if (thing.Data is ModAction)
            {
                return ((ModAction)thing.Data).TargetFullname;
            }
            else
                throw new ArgumentOutOfRangeException();
        }

        public static string GetAuthor(ActivityViewModel viewModel)
        {
            if (viewModel is MessageActivityViewModel)
                return ((MessageActivityViewModel)viewModel).Author;
            else
                return null;
        }

        public static ActivityViewModel CreateActivity(Thing thing)
        {
            ActivityViewModel result;
			var activityIdentifier = ActivityGroupViewModel.MakeActivityIdentifier(thing);
			if (!SelfStreamViewModel.ActivityLookup.TryGetValue(activityIdentifier, out result))
            {
                if (thing.Data is Link)
                    result = new PostedLinkActivityViewModel(thing.Data as Link);
                else if (thing.Data is Comment)
                    result = new PostedCommentActivityViewModel(thing.Data as Comment);
                else if (thing.Data is Message)
                {
                    var messageThing = thing.Data as Message;
                    if (messageThing.WasComment)
                    {
                        result = new RecivedCommentReplyActivityViewModel(messageThing);
                    }
                    //check if its actually mod mail
                    else if (messageThing.Author == "reddit")
                    {
                        result = new ModeratorMessageActivityViewModel(messageThing);
                    }
                    else if (string.IsNullOrEmpty(messageThing.Author))
                    {
                        //this is a deleted sender
                        messageThing.Author = "[deleted]";
                        result = new MessageActivityViewModel(messageThing);
                    }
                    else
                    {
                        result = new MessageActivityViewModel(messageThing);
                    }
                }
                else if (thing.Data is ModAction)
                {
                    result = new ModeratorActivityViewModel(thing.Data as ModAction);
                }
                else
                    throw new ArgumentOutOfRangeException();

				SelfStreamViewModel.ActivityLookup.Add(activityIdentifier, result);

            }
            return result;
        }

        public static void FixupFirstActivity(ActivityViewModel activity, IEnumerable<ActivityViewModel> siblings)
        {
            if (activity is PostedCommentActivityViewModel)
            {
                foreach (var sibling in siblings)
                {
                    if (sibling is RecivedCommentReplyActivityViewModel)
                    {
                        ((PostedCommentActivityViewModel)activity).Subject = ((RecivedCommentReplyActivityViewModel)sibling).Subject;
                        break;
                    }
                }
            }
        }

        public abstract void Tapped();

        public bool IsNew { get; protected set; }
    }

    public class PostedLinkActivityViewModel : ActivityViewModel
    {
        public Link Link { get; private set; }
        public LinkViewModel LinkVM { get; private set; }
        public PostedLinkActivityViewModel(Link link)
        {
            Link = link;
            CreatedUTC = link.CreatedUTC;
            LinkVM = new LinkViewModel(this, link);
			PreviewBody = Body.Length > 100 ? Body.Remove(100) : Body;
			SubTitle = Elipsis(Link.Title, 50);
            IsNew = false;
            var authorTpl = CleanAuthor(link.Author);
            IsSelf = authorTpl.Item2;
            Title = authorTpl.Item1;
        }

        public string Author { get { return Link.Author; } }
        public string Subject { get { return Link.Title; } }
        public string Subreddit { get { return Link.Subreddit; } }
		public string Body { get { return HttpUtility.HtmlDecode(Link.Selftext); } }
        public string LinkTitle { get { return Link.Title; } }

        public override Thing GetThing()
        {
            return new Thing { Kind = "t3", Data = Link };
        }

        public override void Tapped()
        {
            SnooStreamViewModel.NavigationService.NavigateToContentRiver(new SoloContentStreamViewModel(LinkVM));
        }

        public RelayCommand Report { get { return new RelayCommand(() => SnooStreamViewModel.NavigationService.ShowPopup(new ReportReasonInputViewModel(Link), null, CancellationToken.None)); } }
        public RelayCommand Spam { get { return new RelayCommand(async () => await SnooStreamViewModel.RedditService.RemoveThing(Link.Name, true)); } }
        public RelayCommand Approve { get { return new RelayCommand(async () => await SnooStreamViewModel.RedditService.ApproveThing(Link.Name)); } }
        public RelayCommand Remove { get { return new RelayCommand(async () => await SnooStreamViewModel.RedditService.RemoveThing(Link.Name, false)); } }
        public RelayCommand IgnoreReports { get { return new RelayCommand(async () => await SnooStreamViewModel.RedditService.IgnoreReportsOnThing(Link.Name)); } }
        public RelayCommand GotoUserDetails { get { return new RelayCommand(() => SnooStreamViewModel.CommandDispatcher.GotoUserDetails(Link.Author)); } }
    }

    public class PostedCommentActivityViewModel : ActivityViewModel
    {
        private Comment Comment { get; set; }
        private string _body;
        public string Body
        {
            get
            {
                return _body;
            }
            set
            {
                _body = value;
                BodyMD = SnooStreamViewModel.MarkdownProcessor.Process(value);
                RaisePropertyChanged("Body");
                RaisePropertyChanged("BodyMD");
            }
        }

        public string LinkTitle { get { return Comment.LinkTitle; } }
        public string Author { get { return Comment.Author; } }
        public object BodyMD { get; private set; }
        public string Subject { get; set; }
        public string Subreddit { get { return Comment.Subreddit; } }
        public string ParentId { get; private set; }
        public PostedCommentActivityViewModel(Comment comment)
        {
            Comment = comment;
            CreatedUTC = comment.CreatedUTC;
            Body = HttpUtility.HtmlDecode(Comment.Body);
            Subject = SubTitle = Elipsis(comment.LinkTitle, 50);
			PreviewBody = Body.Length > 100 ? Body.Remove(100) : Body;
            IsNew = false;
            var authorTpl = CleanAuthor(comment.Author);
            IsSelf = authorTpl.Item2;
            Title = authorTpl.Item1;
        }

        public override Thing GetThing()
        {
            return new Thing { Kind = "t1", Data = Comment };
        }

        public override void Tapped()
        {
            ActivityViewModel.NavigateToCommentContext(Comment.LinkUrl + Comment.Id + "?context=3", Comment.Name, Comment.Id, Comment.LinkId);
        }

        public RelayCommand Report { get { return new RelayCommand(() => SnooStreamViewModel.NavigationService.ShowPopup(new ReportReasonInputViewModel(Comment), null, CancellationToken.None)); } }
        public RelayCommand Spam { get { return new RelayCommand(async () => await SnooStreamViewModel.RedditService.RemoveThing(Comment.Name, true)); } }
        public RelayCommand Approve { get { return new RelayCommand(async () => await SnooStreamViewModel.RedditService.ApproveThing(Comment.Name)); } }
        public RelayCommand Remove { get { return new RelayCommand(async () => await SnooStreamViewModel.RedditService.RemoveThing(Comment.Name, false)); } }
        public RelayCommand IgnoreReports { get { return new RelayCommand(async () => await SnooStreamViewModel.RedditService.IgnoreReportsOnThing(Comment.Name)); } }
        public RelayCommand GotoUserDetails { get { return new RelayCommand(() => SnooStreamViewModel.CommandDispatcher.GotoUserDetails(Comment.Author)); } }
	}

    public class RecivedCommentReplyActivityViewModel : ActivityViewModel
    {
        private Message Message { get; set; }
        private string _body;

        public RecivedCommentReplyActivityViewModel(Message messageThing)
        {
            Message = messageThing;
            CreatedUTC = messageThing.CreatedUTC;
            Body = HttpUtility.HtmlDecode(Message.Body);
			PreviewBody = Body.Length > 100 ? Body.Remove(100) : Body;
			SubTitle = Elipsis(Subject, 50);
            IsNew = Message.New;
            var authorTpl = CleanAuthor(Author);
            IsSelf = authorTpl.Item2;
            Title = authorTpl.Item1;
        }
        public string Body
        {
            get
            {
                return _body;
            }
            set
            {
                _body = value;
                BodyMD = SnooStreamViewModel.MarkdownProcessor.Process(value);
                RaisePropertyChanged("Body");
                RaisePropertyChanged("BodyMD");
            }
        }
        public string Subreddit { get { return Message.Subreddit; } }
        public string Author { get { return Message.Author; } }
        public object BodyMD { get; private set; }
        public string Subject { get { return Message.LinkTitle; } }
        public string ParentId { get; private set; }

        public override Thing GetThing()
        {
            return new Thing { Kind = "t4", Data = Message };
        }
        public override void Tapped()
        {
            ActivityViewModel.NavigateToCommentContext("http://reddit.com" + Message.Context, Message.Name, Message.Id, null);
        }
    }

    public class MentionActivityViewModel : ActivityViewModel
    {
		public MentionActivityViewModel()
		{
			//PreviewBody = Body.Length > 100 ? Body.Remove(100) : Body;
			//PreviewTitle = Elipsis(messageThing.Author, 50);
		}

        private Message Message { get; set; }
        private string _body;
        public string Body
        {
            get
            {
                return _body;
            }
            set
            {
                _body = value;
                BodyMD = SnooStreamViewModel.MarkdownProcessor.Process(value);
                RaisePropertyChanged("Body");
                RaisePropertyChanged("BodyMD");
            }
        }

        public string Author { get { return Message.Author; } }
        public object BodyMD { get; private set; }
        public string Subject { get; private set; }
        public string ParentId { get; private set; }

        public override Thing GetThing()
        {
            return new Thing { Kind = "t4", Data = Message };
        }

        public override void Tapped()
        {
            SnooStreamViewModel.NavigationService.NavigateToComments(new CommentsViewModel(null, Message.Context));
        }
    }

    public class MessageActivityViewModel : ActivityViewModel
    {
        private string _body;
        private Message MessageThing;

        public MessageActivityViewModel(Message messageThing)
        {
            MessageThing = messageThing;
            CreatedUTC = messageThing.CreatedUTC;
            Body = messageThing.Body;
            IsNew = MessageThing.New;
			PreviewBody = Body.Length > 100 ? Body.Remove(100) : Body;
			SubTitle = Elipsis(messageThing.Subject, 50);
            var authorTpl = CleanAuthor(Author);
            IsSelf = authorTpl.Item2;
            Title = authorTpl.Item1;
        }
        public string Body
        {
            get
            {
                return _body;
            }
            set
            {
                _body = value;
                BodyMD = SnooStreamViewModel.MarkdownProcessor.Process(value);
                RaisePropertyChanged("Body");
                RaisePropertyChanged("BodyMD");
            }
        }

        public override Thing GetThing()
        {
            return new Thing { Kind = "t4", Data = MessageThing };
        }

        public string Author { get { return MessageThing.Author; } }
        public object BodyMD { get; private set; }
        public string Subject { get { return MessageThing.Subject; } }
        public string ParentId { get { return MessageThing.ParentId; } }
		public string Destination { get { return MessageThing.Destination; } }
        public override void Tapped()
        {
            SnooStreamViewModel.NavigationService.NavigateToConversation(ActivityViewModel.GetActivityGroupName(GetThing()));
        }

        RelayCommand Report { get { return new RelayCommand(() => SnooStreamViewModel.NavigationService.ShowPopup(new ReportReasonInputViewModel(MessageThing), null, CancellationToken.None)); } }
        RelayCommand Spam { get { return new RelayCommand(async () => await SnooStreamViewModel.RedditService.RemoveThing(MessageThing.Name, true)); } }
        RelayCommand Remove { get { return new RelayCommand(async () => await SnooStreamViewModel.RedditService.RemoveThing(MessageThing.Name, false)); } }
        
    }

    public class ModeratorActivityViewModel :ActivityViewModel
    {
        private ModAction ModAction;

        public ModeratorActivityViewModel(ModAction modAction)
        {
            ModAction = modAction;
            CreatedUTC = modAction.CreatedUTC;
            IsNew = false;
			IsSelf = false;
        }

        public override Thing GetThing()
        {
            return new Thing { Kind = "modaction", Data = ModAction };
        }

        public override void Tapped()
        {
            throw new NotImplementedException();
        }
    }

    public class ModeratorMessageActivityViewModel :ActivityViewModel
    {
        private Message MessageThing;

        public ModeratorMessageActivityViewModel(Message messageThing)
        {
            CreatedUTC = messageThing.CreatedUTC;
            MessageThing = messageThing;
			PreviewBody = messageThing.Body.Length > 100 ? messageThing.Body.Remove(100) : messageThing.Body;
			SubTitle = Elipsis(messageThing.Subject, 50);
            IsNew = MessageThing.New;
            var authorTpl = CleanAuthor(messageThing.Author);
            IsSelf = authorTpl.Item2;
            Title = authorTpl.Item1;
        }

        public override Thing GetThing()
        {
            return new Thing { Kind = "t4", Data = MessageThing };
        }

        public override void Tapped()
        {
            throw new NotImplementedException();
        }
    }

    public class ReportActivityViewModel : ActivityViewModel
    {
        private static int _uniqueifier = 1;
        public ReportActivityViewModel(string[] reportInfo, bool isMod)
        {
            Title = (isMod ? "[Mod]" : "[User]") + reportInfo[1];
            SubTitle = reportInfo[0];
            CreatedUTC = DateTime.UtcNow.Subtract(TimeSpan.FromDays(9001).Add(TimeSpan.FromTicks(_uniqueifier++)));
        }

        public override Thing GetThing()
        {
            return new Thing { Data = new ThingData { Id = "", Name = "" } };
        }

        public override void Tapped()
        {
            
        }
    }
}
