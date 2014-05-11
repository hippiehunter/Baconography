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
using Windows.ApplicationModel.Store;
using SnooStream.ViewModel;
using System.IO;

namespace SnooStreamWP8.View.Controls
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

		private void Content_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			var settingsViewModel = this.DataContext as SettingsViewModel;
			SnooStreamViewModel.NavigationService.NavigateToContentSettings(settingsViewModel);
		} 
    }
}
