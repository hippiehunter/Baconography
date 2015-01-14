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

		private void Link_Tap (object sender, TappedRoutedEventArgs e)
		{
			var vm = this.DataContext as CommentsViewModel;
			if(vm != null)
				vm.GotoLink.Execute(null);
		}

		private void ReplyButton_Tap(object sender, TappedRoutedEventArgs e)
		{
			var vm = this.DataContext as CommentsViewModel;
			if(vm != null)
				vm.GotoReply.Execute(null);
		}

		private void EditPostButton_Tap(object sender, TappedRoutedEventArgs e)
		{
			var vm = this.DataContext as CommentsViewModel;
			if(vm != null)
				vm.GotoEditPost.Execute(null);
		}

		private void MenuSort_Click (object sender, RoutedEventArgs e)
		{
			double height = 480;
			double width = 325;

			if(LayoutRoot.ActualHeight <= 480)
				height = LayoutRoot.ActualHeight;

			sortPopup.Height = height;
			sortPopup.Width = width;

			var commentsViewModel = DataContext as CommentsViewModel;
			if(commentsViewModel == null)
				return;


			var child = sortPopup.Child as SelectSortTypeView;
			if(child == null)
				child = new SelectSortTypeView();
			child.SortOrder = commentsViewModel.Sort;
			child.Height = height;
			child.Width = width;
			child.button_ok.Click += (object buttonSender, RoutedEventArgs buttonArgs) =>
			{
				commentsViewModel.SetSort(child.SortOrder);
				SnooApplicationPage.Current.PopNavState();
			};

			child.button_cancel.Click += (object buttonSender, RoutedEventArgs buttonArgs) =>
			{
                SnooApplicationPage.Current.PopNavState();
			};

			sortPopup.Child = child;
            SnooApplicationPage.Current.PushNavState(this, "ShowSortPopup");
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			//var context = DataContext as CommentsViewModel;
			//if (context != null)
			//{
			//	context.ViewHack = (item) => commentsList.BringIntoView(item);
			//}
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			//var context = DataContext as CommentsViewModel;
			//if (context != null)
			//{
			//	context.ViewHack = null;
			//}
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
                    args.ItemContainer.ContentTemplate = Resources["LoadFullyTemplate"] as DataTemplate;
                }
                else if (args.Item is MoreViewModel)
                {
                    //args.ItemContainer.ContentTemplate = Resources["MoreViewTemplate"] as DataTemplate;
                    var more = new MoreCommentsView { DataContext = args.Item };
                    more.SetBinding(UIElement.VisibilityProperty, new Binding { Path = new PropertyPath("IsVisible"), Converter = booleanVisiblityConverter });
                    var contentControl = args.ItemContainer.ContentTemplateRoot as ContentControl;
                    contentControl.Content = more;
                }
                else if (args.Item is CommentViewModel)
                {
                    var comment = new CommentView { DataContext = args.Item };
                    comment.SetBinding(UIElement.VisibilityProperty, new Binding { Path = new PropertyPath("IsVisible"), Converter = booleanVisiblityConverter });
                    var contentControl = args.ItemContainer.ContentTemplateRoot as ContentControl;
                    contentControl.Content = comment;
                    comment.Phase0Load(sender, args);
                    
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
