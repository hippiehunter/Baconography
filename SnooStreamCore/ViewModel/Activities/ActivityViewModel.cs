using GalaSoft.MvvmLight;
using SnooSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public abstract Thing GetThing();
        public DateTime CreatedUTC { get; protected set; }
		public string PreviewTitle { get; set; }
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
                    return messageThing.Subject;
                }
            }
            else if (thing.Data is ModAction)
            {
                return ((ModAction)thing.Data).TargetFullname;
            }
            else
                throw new ArgumentOutOfRangeException();
        }

        public static ActivityViewModel CreateActivity(Thing thing)
        {
            if (thing.Data is Link)
                return new PostedLinkActivityViewModel(thing.Data as Link);
            else if (thing.Data is Comment)
                return new PostedCommentActivityViewModel(thing.Data as Comment);
            else if (thing.Data is Message)
            {
                var messageThing = thing.Data as Message;
                if (messageThing.WasComment)
                {
                    return new RecivedCommentReplyActivityViewModel(messageThing);
                }
                //check if its actually mod mail
                else if (messageThing.Author == "reddit")
                {
                    return new ModeratorMessageActivityViewModel(messageThing);
                }
                else if(string.IsNullOrEmpty(messageThing.Author))
                {
                    //TODO: this is wrong need to find the subreddit name for this thing and set it as the author
                    messageThing.Author = "mod";
                    return new MessageActivityViewModel(messageThing);
                }
                else
                {
                    return new MessageActivityViewModel(messageThing);
                }
            }
            else if (thing.Data is ModAction)
            {
                return new ModeratorActivityViewModel(thing.Data as ModAction);
            }
            else
                throw new ArgumentOutOfRangeException();
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
			PreviewTitle = Elipsis(Link.Title, 50);
        }

        public string Author { get { return Link.Author; } }
        public string Subject { get { return Link.Title; } }
        public string Subreddit { get { return Link.Subreddit; } }
        public string Body { get { return Link.Selftext; } }


        public override Thing GetThing()
        {
            return new Thing { Kind = "t3", Data = Link };
        }
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

        public string Author { get { return Comment.Author; } }
        public object BodyMD { get; private set; }
        public string Subject { get; set; }
        public string Subreddit { get { return Comment.Subreddit; } }
        public string ParentId { get; private set; }

        public PostedCommentActivityViewModel(Comment comment)
        {
            Comment = comment;
            CreatedUTC = comment.CreatedUTC;
            Body = Comment.Body;
            Subject = Comment.Body.Substring(0, Math.Min(Comment.Body.Length, 30));
			PreviewBody = Body.Length > 100 ? Body.Remove(100) : Body;
			PreviewTitle = Elipsis(comment.Author, 50);
        }

        public override Thing GetThing()
        {
            return new Thing { Kind = "t1", Data = Comment };
        }
    }

    public class RecivedCommentReplyActivityViewModel : ActivityViewModel
    {
        private Message Message { get; set; }
        private string _body;

        public RecivedCommentReplyActivityViewModel(Message messageThing)
        {
            Message = messageThing;
            CreatedUTC = messageThing.CreatedUTC;
            Body = Message.Body;
			PreviewBody = Body.Length > 100 ? Body.Remove(100) : Body;
			PreviewTitle = Elipsis(messageThing.Author, 50);
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
			PreviewTitle = Elipsis(messageThing.Author, 50);
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
    }

    public class ModeratorActivityViewModel :ActivityViewModel
    {
        private ModAction ModAction;

        public ModeratorActivityViewModel(ModAction modAction)
        {
            ModAction = modAction;
            CreatedUTC = modAction.CreatedUTC;
        }

        public override Thing GetThing()
        {
            return new Thing { Kind = "modaction", Data = ModAction };
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
			PreviewTitle = Elipsis(messageThing.Author, 50);
        }

        public override Thing GetThing()
        {
            return new Thing { Kind = "t4", Data = MessageThing };
        }
    }
}
