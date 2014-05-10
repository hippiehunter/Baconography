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
    public partial class SnooStreamHub : SnooApplicationPage
    {
        public SnooStreamHub()
        {
            InitializeComponent();
        }

        internal void FocusLinkRiver()
        {
            //new Telerik.Windows.Data.GenericGroupDescriptor<SubredditRiverViewModel, string>()
            pivot.SelectedIndex = 0; //link river is always the first index
        }
    }
}