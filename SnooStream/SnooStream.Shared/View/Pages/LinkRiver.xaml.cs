using System;
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

namespace SnooStreamWP8.View.Pages
{
    public partial class LinkRiver : SnooApplicationPage
    {
        public LinkRiver()
        {
            InitializeComponent();
        }

        private void RadDataBoundListBox_DataRequested(object sender, EventArgs e)
        {
            ((LinkRiverViewModel)DataContext).LoadMore();
        }
    }
}