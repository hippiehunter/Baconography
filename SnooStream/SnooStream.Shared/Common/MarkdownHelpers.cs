using GalaSoft.MvvmLight;
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
            if (obj == null)
                return null;

            if (obj is FrameworkElement)
			{
				var frameworkElement = obj as FrameworkElement;
				if (frameworkElement.DataContext is CommentViewModel)
					return frameworkElement.DataContext as CommentViewModel;
			}
			return FindCommentContext(VisualTreeHelper.GetParent(obj));
		}

		private ViewModelBase FindCommentsContext(DependencyObject obj)
		{
            if (obj == null)
                return null;

			if (obj is FrameworkElement)
			{
				var frameworkElement = obj as FrameworkElement;
                if (frameworkElement.DataContext is CommentsViewModel)
                    return frameworkElement.DataContext as CommentsViewModel;
                else if (frameworkElement.DataContext is LinkViewModel)
                    return frameworkElement.DataContext as LinkViewModel;
			}
			return FindCommentsContext(VisualTreeHelper.GetParent(obj));
		}

		public Windows.Foundation.TypedEventHandler<Windows.UI.Xaml.Documents.Hyperlink, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs> MakeLinkCommand(string url)
		{
			return new Windows.Foundation.TypedEventHandler<Windows.UI.Xaml.Documents.Hyperlink, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs>((link, bla) => 
			{
				var commentContext = FindCommentContext(link);
				var topContext = FindCommentsContext(link);
                if(topContext is LinkViewModel)
				    SnooStreamViewModel.CommandDispatcher.GotoLink(topContext, url);
                else
                    SnooStreamViewModel.CommandDispatcher.GotoLink(Tuple.Create(topContext as CommentsViewModel, commentContext, url), url);
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
