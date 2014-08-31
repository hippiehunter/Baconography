using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using SnooStream.ViewModel;
using Windows.UI.Xaml.Controls;
using Windows.UI.Input;
using Windows.UI.Xaml.Input;

namespace SnooStream.View.Controls
{
    public partial class SelfActivityView : UserControl
    {
        public SelfActivityView()
        {
            InitializeComponent();
        }

        private async void listBox_DataRequested(object sender, EventArgs e)
        {
			if(((SelfViewModel)DataContext).IsLoggedIn)
			{
				if(((SelfViewModel)DataContext).Groups.Count == 0)
					await ((SelfViewModel)DataContext).PullNew();
				else
					await ((SelfViewModel)DataContext).PullOlder();
			}
        }
		private void ctrLoadMore_Tap(object sender, TappedRoutedEventArgs e)
        {
        }
    }
}
