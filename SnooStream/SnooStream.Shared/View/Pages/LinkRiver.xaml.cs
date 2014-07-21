using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using SnooStream.Common;
using SnooStream.ViewModel;

namespace SnooStream.View.Pages
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