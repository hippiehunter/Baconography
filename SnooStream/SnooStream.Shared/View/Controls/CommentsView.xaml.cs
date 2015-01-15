using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using SnooStream.ViewModel;
using Windows.UI.Xaml.Controls;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using SnooStream.Common;
using SnooStream.ViewModel.Popups;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.Xaml.Data;
using SnooStream.Converters;

namespace SnooStream.View.Controls
{
	public partial class CommentsView : UserControl
	{
		public CommentsView ()
		{
			InitializeComponent();
		}

        private static BooleanVisibilityConverter booleanVisiblityConverter = new BooleanVisibilityConverter();

		private void commentsList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
            if (args.InRecycleQueue)
            {
                args.ItemContainer.ContentTemplate = null;
            }
            else if (args.ItemContainer.ContentTemplateRoot == null)
            {
                args.RegisterUpdateCallback(commentsList_ContainerContentChanging);
            }
            else
            {
                if (args.Item is LoadFullCommentsViewModel)
                {
                    var contentControl = args.ItemContainer.ContentTemplateRoot as ContentControl;
                    contentControl.ContentTemplate = Resources["LoadFullyTemplate"] as DataTemplate;
                }
                else if (args.Item is MoreViewModel)
                {
                    var more = new MoreCommentsView { DataContext = args.Item };
                    more.SetBinding(UIElement.VisibilityProperty, new Binding { Path = new PropertyPath("IsVisible"), Converter = booleanVisiblityConverter });
                    var contentControl = args.ItemContainer.ContentTemplateRoot as ContentControl;
                    contentControl.Content = more;
                }
                else if (args.Item is CommentViewModel)
                {
                    CommentView comment;
                    if (args.ItemContainer.ContentTemplateRoot is ContentControl && !(((ContentControl)args.ItemContainer.ContentTemplateRoot).Content is CommentView))
                    {
                        comment = new CommentView { DataContext = args.Item };
                        comment.SetBinding(UIElement.VisibilityProperty, new Binding { Path = new PropertyPath("IsVisible"), Converter = booleanVisiblityConverter });
                        var contentControl = args.ItemContainer.ContentTemplateRoot as ContentControl;
                        contentControl.Content = comment;
                    }
                    else
                    {
                        var contentControl = args.ItemContainer.ContentTemplateRoot as ContentControl;
                        comment = contentControl.Content as CommentView;
                    }

                    bool reregister = false;
                    switch (comment.LoadPhase)
                    {
                        case 0:
                            reregister = comment.Phase0Load(sender, args);
                            break;
                        case 1:
                            reregister = comment.Phase1Load(sender, args);
                            break;
                        case 2:
                            comment.Phase2Load(sender, args);
                            break;
                    }
                    if (reregister)
                        args.RegisterUpdateCallback(commentsList_ContainerContentChanging);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
		}

        private void LoadFully_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var loadFullVM = (sender as Button).DataContext as LoadFullCommentsViewModel;
            loadFullVM.LoadFully();
        }
    }
}
