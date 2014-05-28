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
	public partial class LockScreenSettings : SnooApplicationPage
	{
		public LockScreenSettings()
		{
			InitializeComponent();
		}

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // If we have lockscreens, do nothing

            // If we don't, and have access to network, fetch one

            // If we have nothing, use default

            var vm = this.DataContext as SettingsViewModel;
            if (vm != null)
            {
                vm.LockScreen = new LockScreenViewModel();
                vm.LockScreen.SelectedImage = "/Assets/RainbowGlass.jpg";
            }
        }

        public static readonly DependencyProperty IsLockScreenProviderProperty =
            DependencyProperty.Register(
                "IsLockScreenProvider", typeof(bool),
                typeof(LockScreenSettings),
                new PropertyMetadata(false)
            );

        public bool IsLockScreenProvider
        {
            get
            {
                return Windows.Phone.System.UserProfile.LockScreenManager.IsProvidedByCurrentApplication;
            }
        }
	}
}