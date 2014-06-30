using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
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
            AuthorFlairText = _comment.AuthorFlairText;
            Body = _comment.Body;
        }

        public string Id
        {
            get
            {
                return _comment.Id;
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
                BodyMD = SnooStreamViewModel.MarkdownProcessor.Process(value);
                RaisePropertyChanged("Body");
                RaisePropertyChanged("BodyMD");
            }
        }

        public object BodyMD { get; private set; }

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
				Touch();
            }
        }

		// Cause UI to re-evaluate visibility without changing values
		public void Touch()
		{
			var decendents = _context.Decendents(Id);
			if (decendents != null)
			{
				foreach (var child in decendents)
				{
					var comment = child as CommentViewModel;
					var more = child as MoreViewModel;
					if (comment != null) comment.TouchImpl();
					if (more != null) more.TouchImpl();
				}
				
			}
			RaisePropertyChanged("IsVisible");

			if (_context.ViewHack != null)
				_context.ViewHack(this);
		}
		internal void TouchImpl()
		{
			RaisePropertyChanged("IsVisible");
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

        public bool IsEditable
        {
            get
            {
                return string.Compare(SnooStreamViewModel.RedditService.CurrentUserName, PosterName, StringComparison.CurrentCultureIgnoreCase) == 0;
            }
        }

        public AuthorFlairKind PosterFlair
        {
            get
            {
                return SnooStreamViewModel.RedditService.GetUsernameModifiers(PosterName, _comment.LinkId, _comment.SubredditId);
            }
        }

        public RelayCommand MinimizeCommand { get { return new RelayCommand(() => IsMinimized = !IsMinimized); } }
        public RelayCommand GotoContext { get { return new RelayCommand(GotoContextImpl); } }
        public RelayCommand GotoFullLink { get { return new RelayCommand(GotoFullLinkImpl); } }
        public RelayCommand Report { get { return new RelayCommand(ReportImpl); } }
        public RelayCommand Save { get { return new RelayCommand(SaveImpl); } }
        public RelayCommand GotoReply { get { return new RelayCommand(GotoReplyImpl); } }
        public RelayCommand GotoEdit { get { return new RelayCommand(GotoEditImpl); } }
        public RelayCommand GotoUserDetails { get { return new RelayCommand(GotoUserDetailsImpl); } }

        private void GotoContextImpl()
        {
            SnooStreamViewModel.CommandDispatcher.GotoCommentContext(_context, this);
        }

        private void GotoFullLinkImpl()
        {
			SnooStreamViewModel.CommandDispatcher.GotoFullComments(_context, this);
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
    }
}
