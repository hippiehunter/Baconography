using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SnooStream.Services;
using SnooStream.ViewModel.Content;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
	public class CommentsContentStreamViewModel : ViewModelBase, IHasLinks
	{
		public CommentsContentStreamViewModel(CommentsViewModel context)
		{
			Links = new ObservableCollection<ILinkViewModel>();
            if (context.Link != null && context.Link.IsSelfPost)
                ProcessMarkdown(context.Link, context.Link.SelfText);
			foreach(var flatComment in context.FlatComments)
			{
				if(flatComment is CommentViewModel)
				{
					ProcessMarkdown(flatComment, SnooStreamViewModel.MarkdownProcessor.Process(((CommentViewModel)flatComment).Body));
				}
			}
		}

        private void ProcessMarkdown(object parent, MarkdownData markdown)
        {
            if (markdown != null)
            {
                var commentLinks = SnooStreamViewModel.MarkdownProcessor.GetLinks(markdown);
                foreach (var link in commentLinks)
                {
                    if (parent is CommentViewModel)
                    {
                        var madeCommentLink = new CommentLinkViewModel(parent as CommentViewModel, link.Key, link.Value);
                        Links.Add(madeCommentLink);
                    }
                    else if (parent is LinkViewModel)
                    {
                        var madeCommentLink = new SelfTextLinkViewModel(parent as LinkViewModel, link.Key, link.Value);
                        Links.Add(madeCommentLink);
                    }
                }
            }
        }

		public ObservableCollection<ILinkViewModel> Links { get; set; }
		ILinkViewModel _currentSelected;
		public ILinkViewModel CurrentSelected
		{
			get
			{
				return _currentSelected;
			}
			set
			{
				_currentSelected = value;
			}
		}
		private class CommentLinkViewModel : ILinkViewModel
		{
			public CommentLinkViewModel(CommentViewModel context, string url, string title)
			{
				AuthorFlairText = context.Thing.AuthorFlairText;
				Author = context.Thing.Author;
				Subreddit = context.Thing.Subreddit;
				Title = title;
				Url = url;
				CreatedUTC = context.Thing.CreatedUTC;
				Id = context.Thing.Id;
				Votable = context.Votable;
				GotoWeb = new RelayCommand(() =>
					{
						SnooStreamViewModel.NavigationService.NavigateToWeb(Url);
					});
				GotoComments = new RelayCommand(() => 
					{
						var localTemp = Url;
						SnooStreamViewModel.NavigationService.GoBack();
					});
				GotoUserDetails = context.GotoUserDetails;
				LazyContent = new Lazy<ContentViewModel>(() => SnooStream.ViewModel.Content.ContentViewModel.MakeContentViewModel(url, title, this, null));
			}

			public SnooSharp.AuthorFlairKind AuthorFlair { get; set; }

			public string AuthorFlairText { get; set; }

			public bool HasAuthorFlair { get; set; }

			public string Author { get; set; }

			public string Subreddit { get; set; }

			public string Title { get; set; }

			public string Url { get; set; }

			public DateTime CreatedUTC { get; set; }

			public string Id { get; set; }

			Lazy<ContentViewModel> LazyContent;
			public ContentViewModel Content
			{
				get
				{
					return LazyContent.Value;
				}
			}

			public VotableViewModel Votable { get; set; }
			public RelayCommand GotoWeb { get; set; }
			public RelayCommand GotoComments { get; set; }
			public RelayCommand GotoUserDetails { get; set; }
		}

        private class SelfTextLinkViewModel : ILinkViewModel
        {
            public SelfTextLinkViewModel(LinkViewModel context, string url, string title)
            {
                AuthorFlairText = context.Link.AuthorFlairText;
                Author = context.Link.Author;
                Subreddit = context.Link.Subreddit;
                Title = title;
                Url = url;
                CreatedUTC = context.Link.CreatedUTC;
                Id = context.Link.Id;
                Votable = context.Votable;
                GotoWeb = new RelayCommand(() =>
                {
                    SnooStreamViewModel.NavigationService.NavigateToWeb(Url);
                });
                GotoComments = new RelayCommand(() =>
                {
                    var localTemp = Url;
                    SnooStreamViewModel.NavigationService.GoBack();
                });
                GotoUserDetails = context.GotoUserDetails;
                LazyContent = new Lazy<ContentViewModel>(() => SnooStream.ViewModel.Content.ContentViewModel.MakeContentViewModel(url, title, this, null));
            }

            public SnooSharp.AuthorFlairKind AuthorFlair { get; set; }

            public string AuthorFlairText { get; set; }

            public bool HasAuthorFlair { get; set; }

            public string Author { get; set; }

            public string Subreddit { get; set; }

            public string Title { get; set; }

            public string Url { get; set; }

            public DateTime CreatedUTC { get; set; }

            public string Id { get; set; }

            Lazy<ContentViewModel> LazyContent;
            public ContentViewModel Content
            {
                get
                {
                    return LazyContent.Value;
                }
            }

            public VotableViewModel Votable { get; set; }
            public RelayCommand GotoWeb { get; set; }
            public RelayCommand GotoComments { get; set; }
            public RelayCommand GotoUserDetails { get; set; }
        }
	}
}
