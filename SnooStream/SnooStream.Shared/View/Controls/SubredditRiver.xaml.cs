using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using SnooStream.ViewModel;
using GalaSoft.MvvmLight;
using Windows.UI.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Core;
using Windows.UI.Xaml.Input;
using SnooStream.Common;
using System.Threading;
using SnooStream.ViewModel.Popups;

namespace SnooStream.View.Controls
{
    public partial class SubredditRiver : UserControl
    {
        public SubredditRiver()
        {
            InitializeComponent();
        }

        private void manualBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                this.Focus(FocusState.Programmatic);
                var textBox = sender as TextBox;
                var searchText = textBox.Text;
                textBox.Text = "";
                SnooStreamViewModel.CommandDispatcher.GotoSubreddit(searchText);
            }
            else
            {
                BindingExpression bindingExpression = ((TextBox)sender).GetBindingExpression(TextBox.TextProperty);
                if (bindingExpression != null)
                {
                    bindingExpression.UpdateSource();
                }
            }
        }

		private void listBox_ItemTap(object sender, RoutedEventArgs e)
        {
            SnooApplicationPage.Current.PopNavState();
            searchBox.Text = "";
            var linkRiver = ((Button)sender).DataContext as SubredditRiverViewModel.SubredditWrapper;
			if(linkRiver != null)
				SnooStreamViewModel.NavigationService.NavigateToLinkRiver(linkRiver.LinkRiver);
		}

		private void searchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
            BindingExpression bindingExpression = ((TextBox)sender).GetBindingExpression(TextBox.TextProperty);
            if (bindingExpression != null)
            {
                bindingExpression.UpdateSource();
            }
		}

        CancellationTokenSource _contextPopup = new CancellationTokenSource();
        private void Button_Holding(object sender, HoldingRoutedEventArgs e)
        {
            var linkRiver = ((Button)sender).DataContext as SubredditRiverViewModel.SubredditWrapper;
            if (linkRiver != null && e.HoldingState == HoldingState.Started)
            {
                SnooStreamViewModel.NavigationService.ShowPopup(new CommandViewModel
                {
                    Commands = linkRiver.MakeSubredditManagmentCommands(e)
                }, e, _contextPopup.Token);
                e.Handled = true;
            }
            else if (linkRiver != null && e.HoldingState == HoldingState.Canceled)
            {
                _contextPopup.Cancel();
                _contextPopup = new CancellationTokenSource();
            }

        }
    }
}
