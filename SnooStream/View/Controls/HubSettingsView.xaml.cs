using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using Windows.ApplicationModel.Store;
using SnooStream.ViewModel;
using System.IO;
using Windows.UI.Xaml.Controls;
using Windows.UI.Input;
using SnooStream.Common;
using Windows.UI.Xaml.Input;

namespace SnooStream.View.Controls
{
    public partial class HubSettingsView : UserControl
    {
        public HubSettingsView()
        {
            InitializeComponent();
        }

		private void Content_Tap(object sender, TappedRoutedEventArgs e)
		{
			var settingsViewModel = this.DataContext as SettingsViewModel;
			SnooStreamViewModel.NavigationService.NavigateToContentSettings(settingsViewModel);
		}

		private void LockScreen_Tap(object sender, TappedRoutedEventArgs e)
        {
            var settingsViewModel = this.DataContext as SettingsViewModel;
            SnooStreamViewModel.NavigationService.NavigateToLockScreenSettings(settingsViewModel);
        }

		private async void AdFreeUpgrade_Tap(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                ListingInformation products = await CurrentApp.LoadListingInformationAsync();

                // get specific in-app product by ID
                ProductListing productListing = null;
                if (!products.ProductListings.TryGetValue("SnooStreamUpgrade", out productListing))
                {
                    await MessageBox.ShowAsync("Could not find product information", "", MessageBoxButton.OK);
                    return;
                }

                // start product purchase
                await CurrentApp.RequestProductPurchaseAsync(productListing.ProductId, false);
                var enabledAds = !(CurrentApp.LicenseInformation != null && CurrentApp.LicenseInformation.ProductLicenses.ContainsKey("SnooStreamUpgrade"));
                ((Button)sender).IsEnabled = enabledAds;
                SnooStreamViewModel.Settings.AllowAdvertising = enabledAds;
            }
            catch (Exception)
            {
				var result = MessageBox.ShowAsync("Could not complete in app purchase", "", MessageBoxButton.OK);
            }

        }
    }
}
