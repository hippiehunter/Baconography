using GalaSoft.MvvmLight;
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
			foreach(var flatComment in context.FlatComments)
			{
				if(flatComment is CommentViewModel)
				{
					var markdown = SnooStreamViewModel.MarkdownProcessor.Process(((CommentViewModel)flatComment).Body);
					var commentLinks = SnooStreamViewModel.MarkdownProcessor.GetLinks(markdown);
					foreach(var link in commentLinks)
					{
						var madeCommentLink = new CommentLinkViewModel(flatComment as CommentViewModel, link.Key, link.Value);
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
		}
	}
}
