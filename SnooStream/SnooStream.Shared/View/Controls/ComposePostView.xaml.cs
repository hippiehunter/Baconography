using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using SnooStream.ViewModel;
using Windows.UI.Xaml.Controls;
using Windows.UI.Core;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml;
using SnooStream.Common;

namespace SnooStream.View.Controls
{
    public partial class ComposePostView : UserControl
    {
        public ComposePostView()
        {
            InitializeComponent();
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            BindingExpression bindingExpression = ((TextBox)sender).GetBindingExpression(TextBox.TextProperty);
            if (bindingExpression != null)
            {
                bindingExpression.UpdateSource();
            }
        }

        private void Send_Click(object sender, EventArgs e)
        {
            var vm = this.DataContext as PostViewModel;
            if (vm != null)
            {
                var pivotItem = pivot.SelectedItem as PivotItem;
                if (pivotItem != null)
                {
                    vm.Kind = pivotItem.Header as string;
                }
                vm.Submit.Execute(null);
            }
        }

        private void ChangeUser_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as CreateMessageViewModel;
            if (vm != null)
            {
                SnooStreamViewModel.CommandDispatcher.GotoLogin(vm);
            }
        }

        private async void Cancel_Click(object sender, EventArgs e)
        {
            var result = await MessageBox.ShowAsync("Cancel this new post?", "confirm", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                SnooStreamViewModel.NavigationService.GoBack();
            }
        }
    }
}
