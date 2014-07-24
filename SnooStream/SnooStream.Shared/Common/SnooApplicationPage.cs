using GalaSoft.MvvmLight.Messaging;
using SnooStream.Messages;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
			try
			{
				_orientationManager = Application.Current.Resources["orientationManager"] as OrientationManager;
				Messenger.Default.Register<SettingsChangedMessage>(this, OnSettingsChanged);
				OnSettingsChanged(null);
			}
			catch
			{
			}
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

		private string GetStateGuid(string query)
		{
			if (query != null && query.Contains("state="))
			{
				var splitQuery = query.Split('=').ToList();
				return splitQuery[splitQuery.IndexOf("state") + 1];
			}
			else
				return null;

		}

        string _stateGuid;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            
			try
			{
				var validParameters = Frame.ForwardStack
					.Concat(Frame.BackStack)
					.Select(stackEntry => GetStateGuid(stackEntry.Parameter as string))
					.ToList();

				if (e.Parameter is string && !string.IsNullOrWhiteSpace((string)e.Parameter))
				{
					_stateGuid = GetStateGuid(e.Parameter as string);
					validParameters.Add(_stateGuid);
				}

				var parameterHash = new HashSet<string>(validParameters);
				SnooStreamViewModel.NavigationService.ValidateStates(parameterHash);

				AdjustForOrientation(ApplicationView.GetForCurrentView().Orientation);

				if (_stateGuid != null)
				{
					DataContext = NavigationStateUtility.GetDataContext(_stateGuid);
				}
			}
			catch(Exception ex)
			{
				Debug.WriteLine(ex.ToString());
			}
			base.OnNavigatedTo(e);
        }

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
		}

	}
}
