using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using NBoilerpipePortable.Util;
using SnooSharp;
using SnooStream.Model;
using SnooStream.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class CommentViewModel : ViewModelBase
    {
		CommentsViewModel _context;
        Comment _comment;
        VotableViewModel _votable;
        string _linkId;

        public CommentViewModel(CommentsViewModel context, Comment comment, string linkId, int depth = 0)
        {
			_context = context;
            _isMinimized = false;
            _comment = comment;
            _linkId = linkId;
            Depth = depth;
            AuthorFlair = SnooStreamViewModel.RedditService.GetUsernameModifiers(_comment.Author, _linkId, _comment.Subreddit);
            AuthorFlairText = HttpUtility.HtmlDecode(_comment.AuthorFlairText);
			Body = HttpUtility.HtmlDecode(_comment.Body);
        }

        public string Id
        {
            get
            {
                return _comment.Id;
            }
            internal set
            {
                _comment.Id = value;
                _comment.Name = "t1_" + value;
            }
        }

        public VotableViewModel Votable
        {
            get
            {
                if (_votable == null)
                    _votable = new VotableViewModel(_comment, () => RaisePropertyChanged("Votable"));
                return _votable;
            }
        }

        public int Depth { get; set; }

        AuthorFlairKind AuthorFlair { get; set; }

        public string AuthorFlairText { get; set; }

        public bool HasAuthorFlair
        {
            get
            {
                return (!String.IsNullOrWhiteSpace(AuthorFlairText));
            }
        }

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
                RaisePropertyChanged("Body");
                RaisePropertyChanged("BodyMD");
            }
        }

        public object BodyMD
        {
            get
            {
                return SnooStreamViewModel.MarkdownProcessor.Process(Body).MarkdownDom;
            }
        }

        public string PosterName
        {
            get
            {
                return _comment.Author;
            }
        }

        private bool _isMinimized;
        public bool IsMinimized
        {
            get
            {
                return _isMinimized;
            }
            set
            {
                _isMinimized = value;
                RaisePropertyChanged("IsMinimized");
				if (_isMinimized)
					_context.HideDecendents(Id);
				else
					_context.ShowDecendents(this);
            }
        }

		public CommentViewModel Parent
		{
			get
			{
				var parentId = _comment.ParentId.StartsWith("t1_") ? _comment.ParentId.Substring(3) : null;
				if (!string.IsNullOrWhiteSpace(parentId))
				{
					return _context.GetById(parentId);
				}
				else
					return null;
			}
		}

		public bool IsVisible
		{
			get
			{
				var parent = Parent;
				if (parent != null)
				{
					return parent.IsVisible ? !parent.IsMinimized : false;
				}
				return true;
			}
		}

        public bool CanEdit
        {
            get
            {
                return string.Compare(SnooStreamViewModel.RedditService.CurrentUserName, PosterName, StringComparison.CurrentCultureIgnoreCase) == 0;
            }
        }

		public bool CanDelete
		{
			get
			{
				return string.Compare(SnooStreamViewModel.RedditService.CurrentUserName, PosterName, StringComparison.CurrentCultureIgnoreCase) == 0;
			}
		}

        CommentReplyViewModel _replyViewModel;
        public CommentReplyViewModel ReplyViewModel
        {
            get
            {
                if (_replyViewModel == null)
                    _replyViewModel = new CommentReplyViewModel(this, new Thing { Kind = "t1", Data = Thing }, Thing.Id.Length > 5);

                return _replyViewModel;
            }
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get
            {
                return _isEditing;
            }
            set
            {
                _isEditing = value;
                RaisePropertyChanged("IsEditing");
            }
        }

        public AuthorFlairKind PosterFlair
        {
            get
            {
                return SnooStreamViewModel.RedditService.GetUsernameModifiers(PosterName, _comment.LinkId, _comment.SubredditId);
            }
        }

        public RelayCommand ShareContext { get { return new RelayCommand(ShareContextImpl); } }
        public RelayCommand MinimizeCommand { get { return new RelayCommand(() => IsMinimized = !IsMinimized); } }
        public RelayCommand GotoContext { get { return new RelayCommand(GotoContextImpl); } }
        public RelayCommand GotoFullLink { get { return new RelayCommand(GotoFullLinkImpl); } }
        public RelayCommand Report { get { return new RelayCommand(ReportImpl); } }
        public RelayCommand Save { get { return new RelayCommand(SaveImpl); } }
        public RelayCommand GotoReply { get { return new RelayCommand(GotoReplyImpl); } }
        public RelayCommand Edit { get { return new RelayCommand(GotoEditImpl); } }
		public RelayCommand Delete { get { return new RelayCommand(DeleteImpl); } }

		private async void DeleteImpl()
		{
			await SnooStreamViewModel.NotificationService.Report("deleting comment", async () => await SnooStreamViewModel.RedditService.DeleteLinkOrComment(Thing.Name));
		}
        public RelayCommand GotoUserDetails { get { return new RelayCommand(GotoUserDetailsImpl); } }

        private void ShareContextImpl()
        {
            SnooStreamViewModel.SystemServices.ShareLink(_context.BaseUrl + Thing.Id + "?context=3", "Comment in " + _context.Link.Title, "Comment By " + PosterName);
        }

        private void GotoContextImpl()
        {
            SnooStreamViewModel.CommandDispatcher.GotoCommentContext(_context, this);
        }

        private async void GotoFullLinkImpl()
        {
            await _context.LoadFull();
        }

        private void GotoUserDetailsImpl()
        {
            SnooStreamViewModel.CommandDispatcher.GotoUserDetails(_comment.Author);
        }

        private void ReportImpl()
        {
            SnooStreamViewModel.RedditService.AddReportOnThing(_comment.Name);
        }

        private void SaveImpl()
        {
            SnooStreamViewModel.RedditService.AddSavedThing(_comment.Name);
        }

        private void GotoReplyImpl()
        {
			SnooStreamViewModel.CommandDispatcher.GotoReplyToComment(_context, this);
        }

        private void GotoEditImpl()
        {
			SnooStreamViewModel.CommandDispatcher.GotoEditComment(_context, this);
        }

        public Comment Thing
        {
            get
            {
                return _comment;
            }
            //todo this needs to raise events
            set
            {
                _comment = value;
            }
        }

        public void RemoveFromContext()
        {
            _context.RemoveComment(this);
        }

        internal void Rename(string id)
        {
            _context.RenameThing(Thing.Id, id);
        }
    }
}
