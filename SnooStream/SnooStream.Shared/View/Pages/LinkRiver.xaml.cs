﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using SnooStream.Common;
using SnooStream.ViewModel;
using Windows.UI.Xaml.Data;
using GalaSoft.MvvmLight.Messaging;
using SnooStream.Messages;
using Windows.UI.Xaml;

namespace SnooStream.View.Pages
{
    public partial class LinkRiver : SnooApplicationPage
    {
        public LinkRiver()
        {
            InitializeComponent();
        }
		protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			if (e.NavigationMode == Windows.UI.Xaml.Navigation.NavigationMode.Back)
			{
				linksListView.ScrollIntoView(((LinkRiverViewModel)DataContext).LinksViewSource.View.CurrentItem);
			}
		}
    }
}