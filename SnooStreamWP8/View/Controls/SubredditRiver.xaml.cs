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
using GalaSoft.MvvmLight;
using System.Windows.Data;

namespace SnooStreamWP8.View.Controls
{
    public partial class SubredditRiver : UserControl
    {
        public SubredditRiver()
        {
            InitializeComponent();
        }

        private void listBox_ItemTap(object sender, Telerik.Windows.Controls.ListBoxItemTapEventArgs e)
        {
            var linkRiver = e.Item.DataContext as LinkRiverViewModel;
            if (linkRiver != null)
                SnooStreamViewModel.NavigationService.NavigateToLinkRiver(linkRiver);
        }

        private void manualBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                this.Focus();
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

        private void manualBox_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _disableFocusHack = true;
            _needToHackFocus = false;
        }
    }
}
