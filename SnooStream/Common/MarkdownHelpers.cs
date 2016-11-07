using GalaSoft.MvvmLight;
using SnooDom;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace SnooStream.Common
{
    public class MarkdownHelpers : IStyleProvider, ICommandFactory, IMarkdownUtility
    {
        public INavigationContext NavigationContext { get; set; }
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

        private object FindCommentsContext(DependencyObject obj)
        {
            if (obj == null)
                return null;

            if (obj is FrameworkElement)
            {
                var frameworkElement = obj as FrameworkElement;
                if (frameworkElement.DataContext is CommentsViewModel)
                    return frameworkElement.DataContext as CommentsViewModel;
                else if (frameworkElement.DataContext is LinkViewModel)
                    return NavigationContext.MakeCommentContext((frameworkElement.DataContext as LinkViewModel).Thing.Permalink, null, null, (frameworkElement.DataContext as LinkViewModel));
            }
            return FindCommentsContext(VisualTreeHelper.GetParent(obj));
        }

        public Windows.Foundation.TypedEventHandler<Windows.UI.Xaml.Documents.Hyperlink, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs> MakeLinkCommand(string url)
        {
            return new Windows.Foundation.TypedEventHandler<Windows.UI.Xaml.Documents.Hyperlink, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs>((link, bla) =>
            {
                var commentContext = FindCommentContext(link);
                var topContext = FindCommentsContext(link);
                if (topContext is LinkViewModel)
                    Navigation.GotoLink(topContext, url, NavigationContext);
                else
                {
                    var commentsViewModel = topContext as CommentsViewModel;
                    commentsViewModel.Comments.CurrentItem = commentContext;
                    Navigation.GotoLink(topContext as CommentsViewModel, url, NavigationContext);
                }
            });
        }

        public string HTMLDecode(string input, int recurseCount)
        {
            string old = input;
            for (int i = 0; i < recurseCount; i++)
            {
                string current = WebUtility.HtmlDecode(old);
                if (current == old)
                    return old;
                else
                    old = current;
            }
            return old;
        }

        public Windows.UI.Xaml.Style BorderStyle
        {
            get { return Application.Current.Resources["MarkdownBorderStyle"] as Style; }
        }

        public Windows.UI.Xaml.Style RichTextBlockStyle
        {
            get { return Application.Current.Resources["MarkdownRichTextBlockStyle"] as Style; }
        }

        public Windows.UI.Xaml.Style RunStyle
        {
            get { return Application.Current.Resources["MarkdownRunStyle"] as Style; }
        }

        public Windows.UI.Xaml.Style TextBlockStyle
        {
            get { return Application.Current.Resources["MarkdownTextBlockStyle"] as Style; }
        }
    }

    public class SimpleMarkdownContainer
    {
        public SimpleMarkdownContainer(string markdownText)
        {
            Dom = SnooDom.SnooDom.MarkdownToDOM(markdownText ?? string.Empty, _memoryPool);
        }

        public SnooDom.SnooDom Dom { get; private set; }
        private SnooDom.SimpleSessionMemoryPool _memoryPool = new SimpleSessionMemoryPool();
    }
}
