using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Data;
using SnooStream.ViewModel;

namespace SnooStreamWP8.View.Controls
{
    public partial class ComposeMessageView : UserControl
    {
        public ComposeMessageView()
        {
            InitializeComponent();
        }

        private void TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
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

        private void Cancel_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Cancel this new message?", "confirm", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                SnooStreamViewModel.NavigationService.GoBack();
            }
        }
    }
}
