using GalaSoft.MvvmLight.Messaging;
using MetroLog;
using Microsoft.Xaml.Interactivity;
using SnooStream.Messages;
using SnooStream.PlatformServices;
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
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace SnooStream.Common
{
    public class SnooApplicationPage : Page
    {
        OrientationManager _orientationManager;
		ILogger _logger = LogManagerFactory.DefaultLogManager.GetLogger<SnooApplicationPage>();
        public SnooApplicationPage()
        {
			try
			{
				_orientationManager = Application.Current.Resources["orientationManager"] as OrientationManager;
				Messenger.Default.Register<SettingsChangedMessage>(this, OnSettingsChanged);
				OnSettingsChanged(null);
                this.LayoutUpdated += SnooApplicationPage_LayoutUpdated;
			}
			catch
			{
			}
        }

        async void SnooApplicationPage_LayoutUpdated(object sender, object e)
        {
            var orientation = ApplicationView.GetForCurrentView().Orientation;
            if (orientation != LastOrientation)
            {
                LastOrientation = orientation;
                await AdjustForOrientation(orientation);
            }
        }

        public virtual bool DefaultSystray { get { return true; } }
        private ApplicationViewOrientation LastOrientation { get; set; }

#if WINDOWS_PHONE_APP
        protected virtual async Task AdjustForOrientation(ApplicationViewOrientation orientation)
#else
        protected virtual async Task AdjustForOrientation(ApplicationViewOrientation orientation)
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
				_logger.Info("navigating to page " + GetType().Name);
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
					_logger.Info("loading state guid for page " + GetType().Name);
					SystemServices.WrappedCollectionViewSource._dataBinding = true;
					DataContext = NavigationStateUtility.GetDataContext(_stateGuid);
					SystemServices.WrappedCollectionViewSource._dataBinding = false;
					if (DataContext is IRefreshable)
					{
						((IRefreshable)DataContext).MaybeRefresh();
					}
				}
				
			}
			catch(Exception ex)
			{
				_logger.Error("Failed navigating to page " + GetType().Name, ex);
			}
			base.OnNavigatedTo(e);
			_logger.Info("finished loading page " + GetType().Name);
        }

		public bool PopNavState()
		{
            if (_navState.Count > 0)
            {
                var poppedState = _navState.Pop();
                VisualStateManager.GoToState(poppedState.Item1 as Control, poppedState.Item2, true);
                return true;
            }
            return false;
		}

        public bool PopNavState(string popTarget)
        {
            if (_navState.Count > 0 && _navState.Peek().Item2 == popTarget)
            {
                var poppedState = _navState.Pop();
                VisualStateManager.GoToState(poppedState.Item1 as Control, poppedState.Item2, true);
                return true;
            }
            return false;
        }

        public int NavStateCount
        {
            get
            {
                return _navState != null ? _navState.Count : 0;
            }
        }

        Stack<Tuple<object, string>> _navState = new Stack<Tuple<object, string>>();
        internal void PushNavState(object sender, string pushedState)
        {
            //var currentState = VisualStateManager.GetVisualStateGroups(sender as FrameworkElement).First().CurrentState;
            //var currentStateName = currentState != null ? currentState.Name : null;
            VisualStateManager.GoToState(sender as Control, pushedState, false);
            _navState.Push(Tuple.Create(sender, "Normal"));
        }

        public static SnooApplicationPage Current
        {
            get
            {
                return ((Frame)Window.Current.Content).Content as SnooApplicationPage;
            }
        }

        public Popup Popup
        {
            get
            {
                if (Content is Grid)
                {
                    var grid = Content as Grid;
                    var popup = grid.Children.OfType<Popup>().FirstOrDefault();
                    if (popup == null)
                    {
                        popup = new Popup();
                        popup.SetBinding(FrameworkElement.HeightProperty, new Windows.UI.Xaml.Data.Binding { Source = this, Path = new PropertyPath("ActualHeight") });
                        popup.SetBinding(FrameworkElement.WidthProperty, new Windows.UI.Xaml.Data.Binding { Source = this, Path = new PropertyPath("ActualWidth") });
                        grid.Children.Add(popup);
                    }
                    return popup;
                }
                else
                    throw new NotImplementedException();
            }
        }
    }

    public class PushNavState : DependencyObject, IAction
    {
        public string TargetState
        {
            get { return (string)GetValue(TargetStateProperty); }
            set { SetValue(TargetStateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Actions.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TargetStateProperty =
            DependencyProperty.Register("TargetState", typeof(string), typeof(PushNavState), new PropertyMetadata(null));

        public FrameworkElement VisualStateObject
        {
            get { return (FrameworkElement)GetValue(VisualStateObjectProperty); }
            set { SetValue(VisualStateObjectProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VisualStateObject.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VisualStateObjectProperty =
            DependencyProperty.Register("VisualStateObject", typeof(FrameworkElement), typeof(PushNavState), new PropertyMetadata(null));
        

        private T FindAncestor<T>(DependencyObject obj) where T : class
        {
            if (obj == null)
                return null;
            else if (obj is T)
                return obj as T;
            else
                return FindAncestor<T>(VisualTreeHelper.GetParent(obj));
        }

        public object Execute(object sender, object parameter)
        {
            var appPage = FindAncestor<SnooApplicationPage>(sender as DependencyObject);
            if(appPage != null)
                appPage.PushNavState(VisualStateObject, TargetState);
            
            return null;
        }
    }

}
