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

namespace SnooStreamWP8.View.Pages
{
	public partial class LockScreenSettings : SnooApplicationPage
	{
		public LockScreenSettings()
		{
			InitializeComponent();

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
            set { SetValue(IsLockScreenProviderProperty, value); }
        }


        public bool IsLockScreenProvider1
        {
            get
            {
                return Windows.Phone.System.UserProfile.LockScreenManager.IsProvidedByCurrentApplication;
            }
        }
	}
}