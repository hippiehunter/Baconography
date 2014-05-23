using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using SnooStream.ViewModel;

namespace SnooStreamWP8.View.Controls
{
	public partial class CommentsView : UserControl
	{
		public CommentsView ()
		{
			InitializeComponent();
		}

		private async void RadDataBoundListBox_DataRequested (object sender, EventArgs e)
		{
			await ((CommentsViewModel)DataContext).LoadFull();
		}

		private async void RadDataBoundListBox_RefreshRequested (object sender, EventArgs e)
		{
			await ((CommentsViewModel)DataContext).Refresh();
		}

		private void Link_Tap (object sender, System.Windows.Input.GestureEventArgs e)
		{
			var vm = this.DataContext as CommentsViewModel;
			if(vm != null)
				vm.GotoLink.Execute(null);
		}

		private void ReplyButton_Tap (object sender, System.Windows.Input.GestureEventArgs e)
		{
			var vm = this.DataContext as CommentsViewModel;
			if(vm != null)
				vm.GotoReply.Execute(null);
		}

		private void EditPostButton_Tap (object sender, System.Windows.Input.GestureEventArgs e)
		{
			var vm = this.DataContext as CommentsViewModel;
			if(vm != null)
				vm.GotoEditPost.Execute(null);
		}

		private void MenuSort_Click (object sender, EventArgs e)
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
				sortPopup.IsOpen = false;
				commentsViewModel.Sort = child.SortOrder;
			};

			child.button_cancel.Click += (object buttonSender, RoutedEventArgs buttonArgs) =>
			{
				sortPopup.IsOpen = false;
			};

			sortPopup.Child = child;
			sortPopup.IsOpen = true;
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			var context = DataContext as CommentsViewModel;
			if (context != null)
			{
				context.ViewHack = (item) => commentsList.BringIntoView(item);
			}
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			var context = DataContext as CommentsViewModel;
			if (context != null)
			{
				context.ViewHack = null;
			}
		}
	}
}
