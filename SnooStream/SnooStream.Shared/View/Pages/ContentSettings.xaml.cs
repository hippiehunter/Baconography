using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using SnooStream.View.Controls;
using Windows.ApplicationModel.Store;
using SnooStream.ViewModel;
using SnooStream.Common;
using Windows.UI.Xaml;
using Windows.UI.Input;
using Windows.UI.Xaml.Controls;

namespace SnooStream.View.Pages
{
	public partial class ContentSettings : SnooApplicationPage
	{
		public ContentSettings()
		{
			InitializeComponent();
		}

		private void UpdateLockScreenStatus()
		{
			//var isProvider = Windows.Phone.System.UserProfile.LockScreenManager.IsProvidedByCurrentApplication;

			//if (isProvider)
			//{
			//    lockStatus.IsChecked = true;
			//    lockStatus.IsEnabled = false;
			//}
			//else
			//{
			//    lockStatus.IsChecked = false;
			//    lockStatus.IsEnabled = true;
			//}
		}

		protected void OpenHelp(string topic, string content)
		{
			//double height = LayoutRoot.ActualHeight - 24;
			//double width = LayoutRoot.ActualWidth - 24;

			//helpPopup.Height = height;
			//helpPopup.Width = width;

			//var child = helpPopup.Child as HelpView;
			//if (child == null)
			//    child = new HelpView();
			//child.Height = height;
			//child.Width = width;
			//child.Topic = topic;
			//child.Content = content;

			//helpPopup.Child = child;
			//helpPopup.IsOpen = true;
		}

		//protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
		//{
		//if (helpPopup.IsOpen == true)
		//{
		//    helpPopup.IsOpen = false;
		//    e.Cancel = true;
		//}
		//else
		//{
		//    base.OnBackKeyPress(e);

		//}
		//}

		private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
		{
			//var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
			//var hyperlinkButton = e.OriginalSource as ContextDataButton;
			//if (hyperlinkButton != null)
			//{
			//    _navigationService.NavigateToExternalUri(new Uri((string)hyperlinkButton.ContextData));
			//}
		}

		private void OrientationLock_Checked(object sender, RoutedEventArgs e)
		{
			//var preferences = this.DataContext as ContentPreferencesViewModel;
			//if (preferences != null)
			//{
			//    preferences.Orientation = this.Orientation.ToString();
			//}
		}

		private void OrientationLock_Unchecked(object sender, RoutedEventArgs e)
		{
			//var preferences = this.DataContext as ContentPreferencesViewModel;
			//if (preferences != null)
			//{
			//    preferences.Orientation = this.Orientation.ToString();
			//}
		}


		private async void ShowSystemLockScreenSettings(object sender, RoutedEventArgs e)
		{
			await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-lock:"));
		}

		private async void ShowLockScreenPreview(object sender, RoutedEventArgs e)
		{
			//var userService = ServiceLocator.Current.GetInstance<IUserService>();
			//var settingsService = ServiceLocator.Current.GetInstance<ISettingsService>();

			//await Utility.DoActiveLockScreen(settingsService, ServiceLocator.Current.GetInstance<IRedditService>(), userService,
			//    ServiceLocator.Current.GetInstance<IImagesService>(), ServiceLocator.Current.GetInstance<INotificationService>(), true);

			//var lockScreen = new ViewModelLocator().LockScreen;
			//if (!settingsService.UseImagePickerForLockScreen)
			//{
			//    lockScreen.ImageSource = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\" + lockScreen.ImageSource;
			//}

			//var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
			//_navigationService.Navigate<LockScreen>(null);
		}

		enum PickerTask
		{
			None,
			ImagesReddit,
			TopPostReddit,
			LiveTileReddit
		}

		private PickerTask _pickerTask = PickerTask.None;

		private async void CompletePickerTask()
		{
			//var locator = new ViewModelLocator();
			//var spvm = locator.SubredditPicker;
			//var cpvm = locator.ContentPreferences;

			//switch (_pickerTask)
			//{
			//    case PickerTask.None:
			//        break;
			//    case PickerTask.ImagesReddit:
			//        _pickerTask = PickerTask.None;
			//        cpvm.ImagesSubreddit = spvm.GetSubredditString();

			//        SetLockScreen(null, null);
			//        break;
			//    case PickerTask.TopPostReddit:
			//        _pickerTask = PickerTask.None;
			//        cpvm.LockScreenReddit = spvm.GetSubredditString();

			//        SetLockScreen(null, null);
			//        break;
			//    case PickerTask.LiveTileReddit:
			//        _pickerTask = PickerTask.None;
			//        cpvm.LiveTileReddit = spvm.GetSubredditString();
			//        break;
			//    default:
			//        break;
			//}
		}

		private void SelectLockScreenSubreddit(object sender, RoutedEventArgs e)
		{
			//var locator = new ViewModelLocator();
			//var spvm = locator.SubredditPicker;
			//var cpvm = locator.ContentPreferences;
			//_pickerTask = PickerTask.ImagesReddit;

			//cpvm.UseImagePickerForLockScreen = false;
			//spvm.SetSubredditList(cpvm.ImagesSubreddit);

			//for (int i = 0; i < 10; i++)
			//{
			//    if (File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + string.Format("\\lockScreenCache{0}.jpg", i)))
			//    {
			//        File.Delete(Windows.Storage.ApplicationData.Current.LocalFolder.Path + string.Format("\\lockScreenCache{0}.jpg", i));
			//    }
			//}


			//var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
			//_navigationService.Navigate<SubredditPickerPageView>(null);
		}

		private void SelectTopPostSubreddit(object sender, RoutedEventArgs e)
		{
			//var locator = new ViewModelLocator();
			//var spvm = locator.SubredditPicker;
			//var cpvm = locator.ContentPreferences;
			//_pickerTask = PickerTask.TopPostReddit;

			//spvm.SetSubredditList(cpvm.LockScreenReddit);

			//var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
			//_navigationService.Navigate<SubredditPickerPageView>(null);
		}

		private void SelectLiveTileSubreddit(object sender, RoutedEventArgs e)
		{
			//var locator = new ViewModelLocator();
			//var spvm = locator.SubredditPicker;
			//var cpvm = locator.ContentPreferences;
			//_pickerTask = PickerTask.LiveTileReddit;

			//spvm.SetSubredditList(cpvm.LiveTileReddit);

			//var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
			//_navigationService.Navigate<SubredditPickerPageView>(null);
		}

		private async void SetLockScreen(object sender, RoutedEventArgs e)
		{
			//var isProvider = Windows.Phone.System.UserProfile.LockScreenManager.IsProvidedByCurrentApplication;
			//if (sender is CheckBox && isProvider)
			//    return;

			//isProvider = await Utility.RequestLockAccess();
			//UpdateLockScreenStatus();


			//var locator = new ViewModelLocator();
			//var cpvm = locator.ContentPreferences;
			//var settingsService = ServiceLocator.Current.GetInstance<ISettingsService>();

			//if (cpvm.UseImagePickerForLockScreen && File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockScreenCache0.jpg"))
			//{
			//    File.Delete(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockScreenCache0.jpg");
			//}
			//cpvm.UseImagePickerForLockScreen = false;

			//await Utility.DoActiveLockScreen(settingsService, ServiceLocator.Current.GetInstance<IRedditService>(), userService,
			//    ServiceLocator.Current.GetInstance<IImagesService>(), ServiceLocator.Current.GetInstance<INotificationService>(), false);
		}

		public static readonly DependencyProperty ImagePreviewProperty =
			DependencyProperty.Register(
				"ImagePreview",
				typeof(string),
				typeof(ContentSettings),
				new PropertyMetadata(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockScreenCache0.jpg")
			);

		private async void SelectLockScreenImage(object sender, RoutedEventArgs e)
		{
			//var isProvider = await Utility.RequestLockAccess();
			//UpdateLockScreenStatus();

			//Microsoft.Phone.Tasks.PhotoChooserTask picker = new Microsoft.Phone.Tasks.PhotoChooserTask();
			//picker.Completed += picker_Completed;
			//picker.Show();
		}

		//async void picker_Completed(object sender, PhotoResult e)
		//{
			//var settingsService = ServiceLocator.Current.GetInstance<ISettingsService>();
			//var userService = ServiceLocator.Current.GetInstance<IUserService>();
			//var locator = new ViewModelLocator();
			//var cpvm = locator.ContentPreferences;

			//if (e.Error == null && e.ChosenPhoto != null)
			//{
			//    BitmapImage image = new BitmapImage();
			//    image.CreateOptions = BitmapCreateOptions.None;
			//    image.SetSource(e.ChosenPhoto);
			//    Utility.MakeSingleLockScreenFromImage(0, image);
			//    cpvm.UseImagePickerForLockScreen = true;

			//    ClearValue(ImagePreviewProperty);
			//    SetValue(ImagePreviewProperty, Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockScreenCache0.jpg");

			//    await Utility.DoActiveLockScreen(settingsService, ServiceLocator.Current.GetInstance<IRedditService>(), userService,
			//        ServiceLocator.Current.GetInstance<IImagesService>(), ServiceLocator.Current.GetInstance<INotificationService>(), false);
			//}
			//else
			//{
			//    subredditRadioButton.IsChecked = true;
			//}
		//}



		private void HelpOfflineButton_Tap(object sender, TappedEventArgs e)
		{
			OpenHelp(
				"OFFLINE CONTENT",
				"The predictive offline cache aggregates usage statistics about the subreddits, links and comments that you click on. This data is stored only on your device and can be erased at any time. We use this data to intelligently guess which links you are likely to click in order to cache the relevant data locally on your device. When you then click on a cached link, the data is loaded very quickly from your device instead of from the web."
				+ "\r\n\r\n" +
				"Overnight offline cache is an extension of the predictive cache. When your device is plugged in and connected to Wi-Fi, we can safely download more data at a faster rate. If you enable this option, we will run a background process during optimal conditions to download more reddit goodness."
				);
		}

		private async void AdFreeUpgrade_Click(object sender, RoutedEventArgs e)
		{
			var toggleSwitch = sender as ToggleSwitch;
			try
			{
				ListingInformation products = await CurrentApp.LoadListingInformationByProductIdsAsync(new[] { "SnooStreamUpgrade" });

				// get specific in-app product by ID
				ProductListing productListing = null;
				if (!products.ProductListings.TryGetValue("SnooStreamUpgrade", out productListing))
				{
					await MessageBox.ShowAsync("Could not find product information", "", MessageBoxButton.OK);
					if (toggleSwitch != null)
						toggleSwitch.IsOn = false;
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
				if (toggleSwitch != null)
					toggleSwitch.IsOn = false;
			}

		}
	}
}