using SnooDom;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace SnooStream.Common
{
    class MarkdownHelpers : IStyleProvider, ICommandFactory
    {
		private CommentViewModel FindCommentContext(DependencyObject obj)
		{
			if (obj is FrameworkElement)
			{
				var frameworkElement = obj as FrameworkElement;
				if (frameworkElement.DataContext is CommentViewModel)
					return frameworkElement.DataContext as CommentViewModel;
			}
			return FindCommentContext(VisualTreeHelper.GetParent(obj));
		}

		private CommentsViewModel FindCommentsContext(DependencyObject obj)
		{
			if (obj is FrameworkElement)
			{
				var frameworkElement = obj as FrameworkElement;
				if (frameworkElement.DataContext is CommentsViewModel)
					return frameworkElement.DataContext as CommentsViewModel;
			}
			return FindCommentsContext(VisualTreeHelper.GetParent(obj));
		}

		public Windows.Foundation.TypedEventHandler<Windows.UI.Xaml.Documents.Hyperlink, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs> MakeLinkCommand(string url)
		{
			return new Windows.Foundation.TypedEventHandler<Windows.UI.Xaml.Documents.Hyperlink, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs>((link, bla) => 
			{
				var commentContext = FindCommentContext(link);
				var commentsContext = FindCommentsContext(link);
				SnooStreamViewModel.CommandDispatcher.GotoLink(Tuple.Create(commentsContext, commentContext, url), url);
			});
		}

		public Windows.UI.Xaml.Style BorderStyle
		{
            get { return App.Current.Resources["MarkdownBorderStyle"] as Style; }
		}

		public Windows.UI.Xaml.Style RichTextBlockStyle
		{
            get { return App.Current.Resources["MarkdownRichTextBlockStyle"] as Style; }
		}

		public Windows.UI.Xaml.Style RunStyle
		{
            get { return App.Current.Resources["MarkdownRunStyle"] as Style; }
		}

		public Windows.UI.Xaml.Style TextBlockStyle
		{
            get { return App.Current.Resources["MarkdownTextBlockStyle"] as Style; }
		}
	}
}
