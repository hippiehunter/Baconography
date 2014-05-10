using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telerik.Windows.Controls;
using SnooStream.ViewModel;

namespace SnooStreamWP8.View.Controls
{
    public partial class SelfActivityView : UserControl
    {
        public SelfActivityView()
        {
            InitializeComponent();
        }

        private void listBox_IsCheckModeActiveChanged(object sender, IsCheckModeActiveChangedEventArgs e)
        {
        }

        private async void listBox_DataRequested(object sender, EventArgs e)
        {
			if(((SelfViewModel)DataContext).IsLoggedIn)
			{
				if(((SelfViewModel)DataContext).Activities.Count == 0)
					await ((SelfViewModel)DataContext).PullNew();
				else
					await ((SelfViewModel)DataContext).PullOlder();
			}
        }
        private void ctrLoadMore_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
        }
    }
}
