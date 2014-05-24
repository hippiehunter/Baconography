using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Controls;
using SnooStream.Messages;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Navigation;

namespace SnooStreamWP8.Common
{
    public class SnooApplicationPage : PhoneApplicationPage
    {
        OrientationManager _orientationManager;
        public SnooApplicationPage()
        {
            FontSize = (double)Application.Current.Resources["PhoneFontSizeNormal"];
            FontFamily = Application.Current.Resources["PhoneFontFamilyNormal"] as FontFamily;
            Foreground = Application.Current.Resources["PhoneForegroundBrush"] as Brush;
            _orientationManager = Application.Current.Resources["orientationManager"] as OrientationManager;
            Messenger.Default.Register<SettingsChangedMessage>(this, OnSettingsChanged);
			OnSettingsChanged(null);
        }

        public virtual bool DefaultSystray { get { return true; } }

        protected virtual void AdjustForOrientation(PageOrientation orientation)
        {
            switch (orientation)
            {
                case PageOrientation.Landscape:
                case PageOrientation.LandscapeLeft:
                case PageOrientation.LandscapeRight:
                    Microsoft.Phone.Shell.SystemTray.IsVisible = false;
                    _orientationManager.IsLandscape = true;
                    break;
                case PageOrientation.None:
                case PageOrientation.Portrait:
                case PageOrientation.PortraitDown:
                case PageOrientation.PortraitUp:
                default:
                    Microsoft.Phone.Shell.SystemTray.IsVisible = DefaultSystray;
                    _orientationManager.IsLandscape = false;
                    break;
            }
            SnooStreamViewModel.Settings.ScreenHeight = 800;
            SnooStreamViewModel.Settings.ScreenWidth = 480;
        }

        private PageOrientation StringToOrientation(string orientation)
        {
            switch (orientation)
            {
                case "Landscape":
                    return PageOrientation.Landscape;
                case "LandscapeLeft":
                    return PageOrientation.LandscapeLeft;
                case "LandscapeRight":
                    return PageOrientation.LandscapeRight;
                case "Portrait":
                    return PageOrientation.Portrait;
                case "PortraitUp":
                    return PageOrientation.PortraitUp;
                case "PortraitDown":
                    return PageOrientation.PortraitDown;
                case "None":
                default:
                    return PageOrientation.None;
            }
        }

        private bool _orientationLocked = false;
        private void OnSettingsChanged(SettingsChangedMessage message)
        {
            _orientationLocked = SnooStreamViewModel.Settings.OrientationLock;
            var orientation = StringToOrientation(SnooStreamViewModel.Settings.Orientation);

            if (_orientationLocked)
            {
                switch (orientation)
                {
                    case PageOrientation.Landscape:
                    case PageOrientation.LandscapeLeft:
                    case PageOrientation.LandscapeRight:
                        SupportedOrientations = SupportedPageOrientation.Landscape;
                        Orientation = orientation;
                        Microsoft.Phone.Shell.SystemTray.IsVisible = false;
                        break;
                    case PageOrientation.None:
                    case PageOrientation.Portrait:
                    case PageOrientation.PortraitDown:
                    case PageOrientation.PortraitUp:
                    default:
                        SupportedOrientations = SupportedPageOrientation.Portrait;
                        Orientation = orientation;
                        Microsoft.Phone.Shell.SystemTray.IsVisible = DefaultSystray;
                        break;
                }
            }
            else
            {
                SupportedOrientations = SupportedPageOrientation.PortraitOrLandscape;
            }
        }

        string _stateGuid;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            AdjustForOrientation(Orientation);
            var url = e.Uri.ToString();
            if(url.Contains("?"))
            {
                var query = url.Substring(url.IndexOf('?') + 1);
                DataContext = NavigationStateUtility.GetDataContext(query, out _stateGuid);
            }
        }

        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            AdjustForOrientation(e.Orientation);
            base.OnOrientationChanged(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.New && e.Uri.ToString() == "/View/Pages/SnooStreamHub.xaml" && e.IsCancelable)
                e.Cancel = true;
            else
                base.OnNavigatingFrom(e);
        }

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            if(_stateGuid != null)
            {
                SnooStreamViewModel.NavigationService.RemoveState(_stateGuid);
            }
        }
    }
}
