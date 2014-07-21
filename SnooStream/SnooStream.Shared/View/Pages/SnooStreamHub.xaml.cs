using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using SnooStream.Common;
using SnooStream.ViewModel;

namespace SnooStream.View.Pages
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