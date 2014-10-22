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
				SnooStreamViewModel.NavigationService.PopVisualState();
			};

			child.button_cancel.Click += (object buttonSender, RoutedEventArgs buttonArgs) =>
			{
				SnooStreamViewModel.NavigationService.PopVisualState();
			};

			sortPopup.Child = child;
			SnooStreamViewModel.NavigationService.PushVisualState(LayoutRoot, "ShowSortPopup");
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

		private void commentsList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (args.ItemContainer.ContentTemplateRoot is CardLinkView)
			{
				var card = args.ItemContainer.ContentTemplateRoot as CardLinkView;
				card.PhaseLoad(sender, args);
			}
			else if (args.ItemContainer.ContentTemplateRoot is CommentView)
			{
				var comment = args.ItemContainer.ContentTemplateRoot as CommentView;
				comment.DataContext = args.Item;
				comment.PhaseLoad(sender, args);
			}
			else
			{

			}
		}
	}
}
