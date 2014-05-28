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
    public partial class HubSettingsView : UserControl
    {
        public HubSettingsView()
        {
            InitializeComponent();
        }

		private void Content_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			var settingsViewModel = this.DataContext as SettingsViewModel;
			SnooStreamViewModel.NavigationService.NavigateToContentSettings(settingsViewModel);
		}

        private void LockScreen_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var settingsViewModel = this.DataContext as SettingsViewModel;
            SnooStreamViewModel.NavigationService.NavigateToLockScreenSettings(settingsViewModel);
        }

        private async void AdFreeUpgrade_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                ListingInformation products = await CurrentApp.LoadListingInformationByProductIdsAsync(new[] { "SnooStreamWP8Upgrade" });

                // get specific in-app product by ID
                ProductListing productListing = null;
                if (!products.ProductListings.TryGetValue("SnooStreamWP8Upgrade", out productListing))
                {
                    MessageBox.Show("Could not find product information");
                    return;
                }

                // start product purchase
                await CurrentApp.RequestProductPurchaseAsync(productListing.ProductId, false);
                var enabledAds = !(CurrentApp.LicenseInformation != null && CurrentApp.LicenseInformation.ProductLicenses.ContainsKey("SnooStreamWP8Upgrade"));
                ((Button)sender).IsEnabled = enabledAds;
                SnooStreamViewModel.Settings.AllowAdvertising = enabledAds;
            }
            catch (Exception)
            {
                MessageBox.Show("Could not complete in app purchase");
            }

        }
    }
}
