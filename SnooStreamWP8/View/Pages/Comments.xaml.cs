﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using SnooStreamWP8.Common;
using SnooStream.ViewModel;
using SnooStreamWP8.View.Controls;

namespace SnooStreamWP8.View.Pages
{
    public partial class Comments : SnooApplicationPage
    {
        public Comments()
        {
            InitializeComponent();
        }

        private async void RadDataBoundListBox_DataRequested(object sender, EventArgs e)
        {

        }

        private async void RadDataBoundListBox_RefreshRequested(object sender, EventArgs e)
        {
            await ((CommentsViewModel)DataContext).Refresh();
        }

        private void Link_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var vm = this.DataContext as CommentsViewModel;
            if (vm != null)
                vm.GotoLink.Execute(null);
        }

        private void ReplyButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var vm = this.DataContext as CommentsViewModel;
            if (vm != null)
                vm.GotoReply.Execute(null);
        }

        private void EditPostButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var vm = this.DataContext as CommentsViewModel;
            if (vm != null)
                vm.GotoEditPost.Execute(null);
        }

        private void MenuSort_Click(object sender, EventArgs e)
        {
            double height = 480;
            double width = 325;

            if (LayoutRoot.ActualHeight <= 480)
                height = LayoutRoot.ActualHeight;

            sortPopup.Height = height;
            sortPopup.Width = width;

            var commentsViewModel = DataContext as CommentsViewModel;
            if (commentsViewModel == null)
                return;


            var child = sortPopup.Child as SelectSortTypeView;
            if (child == null)
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
    }
}