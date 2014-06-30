using GalaSoft.MvvmLight.Messaging;
using SnooStream.Messages;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace SnooStream.Common
{
    public class SnooApplicationPage : Page
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

#if WINDOWS_PHONE_APP
        protected virtual async void AdjustForOrientation(ApplicationViewOrientation orientation)
#else
        protected virtual void AdjustForOrientation(ApplicationViewOrientation orientation)
#endif
        {
            switch (orientation)
            {
                case ApplicationViewOrientation.Landscape:
#if WINDOWS_PHONE_APP
                    await StatusBar.GetForCurrentView().HideAsync();
#endif
                    break;
                case ApplicationViewOrientation.Portrait:
                default:
#if WINDOWS_PHONE_APP
                    if (DefaultSystray)
                        await StatusBar.GetForCurrentView().ShowAsync();
                    else
                        await StatusBar.GetForCurrentView().HideAsync();
#endif
                    break;
            }

#if WINDOWS_PHONE_APP
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            SnooStreamViewModel.Settings.ScreenHeight = bounds.Height;
            SnooStreamViewModel.Settings.ScreenWidth = bounds.Width;
#endif
        }

        private ApplicationViewOrientation StringToOrientation(string orientation)
        {
            switch (orientation)
            {
                case "Landscape":
                case "LandscapeLeft":
                case "LandscapeRight":
                    return ApplicationViewOrientation.Landscape;
                case "Portrait":
                case "PortraitUp":
                case "PortraitDown":
                    return ApplicationViewOrientation.Portrait;
                case "None":
                default:
                    return ApplicationViewOrientation.Landscape;
            }
        }

        private bool _orientationLocked = false;
        private void OnSettingsChanged(SettingsChangedMessage message)
        {
            _orientationLocked = SnooStreamViewModel.Settings.OrientationLock;
            var orientation = StringToOrientation(SnooStreamViewModel.Settings.Orientation);

            if (_orientationLocked)
            {
                AdjustForOrientation(orientation);
            }
        }

        string _stateGuid;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);


            var validParameters = Frame.ForwardStack
                .Concat(Frame.BackStack)
                .Select(stackEntry => stackEntry.Parameter as string)
                .ToList();

            var parameterHash = new HashSet<string>(validParameters);
            SnooStreamViewModel.NavigationService.ValidateStates(parameterHash);

            AdjustForOrientation(ApplicationView.GetForCurrentView().Orientation);

            if (e.Parameter is string)
            {
                DataContext = NavigationStateUtility.GetDataContext(e.Parameter as string, out _stateGuid);
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            
            //if (e.NavigationMode == NavigationMode.New && e.SourcePageType == typeof(SnooStreamHubView)Uri.ToString() == "/View/Pages/SnooStreamHub.xaml" && e.IsCancelable)
            //    e.Cancel = true;
            //else
                base.OnNavigatingFrom(e);
        }

        //protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        //{
        //    Windows.UI.Xaml.Navigation.PageStackEntry
        //    base.OnRemovedFromJournal(e);
        //    if(_stateGuid != null)
        //    {
        //        SnooStreamViewModel.NavigationService.RemoveState(_stateGuid);
        //    }
        //}
    }
}
