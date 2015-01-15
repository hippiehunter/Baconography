using SnooStream.Services;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SnooStream.View.Controls
{
    public partial class CommentView : UserControl
    {
        public CommentView()
        {
            InitializeComponent();
        }

        public int LoadPhase;

        internal bool Phase0Load(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (!args.InRecycleQueue)
            {
                contentControl.ContentTemplate = null;
                contentControl.Content = null;
                args.Handled = true;
                LoadPhase = 1;
                return true;
            }
            return false;
        }

        internal bool Phase1Load(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (!args.InRecycleQueue)
            {
                if (args.Item is CommentViewModel)
                {
                    contentControl.ContentTemplate = Resources["textTemplate"] as DataTemplate;
                    contentControl.Content = ((CommentViewModel)args.Item).Body;
                    args.Handled = true;
                    LoadPhase = 2;
                    return true;
                }
                else
                    throw new NotImplementedException();
            }
            return false;
        }

        internal bool Phase2Load(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (!args.InRecycleQueue)
            {
                if (args.Item is CommentViewModel)
                {
                    var body = ((CommentViewModel)args.Item).Body;
                    var markdownBody = SnooStreamViewModel.MarkdownProcessor.Process(body);

                    if (!SnooStreamViewModel.MarkdownProcessor.IsPlainText(markdownBody))
                    {
                        contentControl.ContentTemplate = Resources["markdownTemplate"] as DataTemplate;
                        contentControl.Content = markdownBody.MarkdownDom;
                        args.Handled = true;
                    }
                    else if (contentControl.Content == null)
                    {
                        var textContent = (Resources["textTemplate"] as DataTemplate).LoadContent() as FrameworkElement;
                        textContent.DataContext = ((CommentViewModel)args.Item).Body;
                        contentControl.Content = textContent;
                        args.Handled = true;
                    }
                    LoadPhase = 3;
                }
                else
                    throw new NotImplementedException();
            }
            return false;
        }
	}
}
