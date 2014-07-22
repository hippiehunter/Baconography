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

        //this bit of unpleasantry is needed to prevent the input box from getting defocused when an item gets added to the collection
        bool _disableFocusHack = false;
        bool _needToHackFocus = false;
        TextBox _manualBox = null;
        private void manualBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _manualBox = sender as TextBox;
            if (_disableFocusHack)
                _disableFocusHack = false;
            else
            {
                _needToHackFocus = true;
            }
        }

		private void manualBox_MouseLeave(object sender, TappedRoutedEventArgs e)
        {
            _disableFocusHack = true;
            _needToHackFocus = false;
        }

		private void listBox_ItemTap(object sender, TappedRoutedEventArgs e)
		{
			var linkRiver = ((Button)sender).DataContext as LinkRiverViewModel;
			if(linkRiver != null)
				SnooStreamViewModel.NavigationService.NavigateToLinkRiver(linkRiver);
		}
    }
}
