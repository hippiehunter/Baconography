using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using SnooStream.ViewModel;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml;
using Windows.UI.Core;
using SnooStream.Common;
using Windows.UI.Xaml.Input;

namespace SnooStream.View.Controls
{
    public partial class ComposeMessageView : UserControl
    {
        public ComposeMessageView()
        {
            InitializeComponent();
        }

		private void TextBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            BindingExpression bindingExpression = ((TextBox)sender).GetBindingExpression(TextBox.TextProperty);
            if (bindingExpression != null)
            {
                bindingExpression.UpdateSource();
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
            var result = await MessageBox.ShowAsync("Cancel this new message?", "confirm", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                SnooStreamViewModel.NavigationService.GoBack();
            }
        }
    }
}
