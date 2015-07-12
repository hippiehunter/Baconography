using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using SnooStream.Messages;
using SnooStream.PlatformServices;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.UI.Core;
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
        object _dataContext;
        OrientationManager _orientationManager;
        public SnooApplicationPage()
        {
            try
            {
                _orientationManager = Application.Current.Resources["orientationManager"] as OrientationManager;
                Messenger.Default.Register<SettingsChangedMessage>(this, OnSettingsChanged);
                Messenger.Default.Register<FocusChangedMessage>(this, OnFocusChanged);
                OnSettingsChanged(null);
                Loaded += OnLoaded;
#if WINDOWS_PHONE_APP
                ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);
#endif
            }
            catch
            {
            }
        }

        private void OnFocusChanged(FocusChangedMessage obj)
        {
            if (_dataContext != null)
            {
                if (obj.Sender == _dataContext && _dataContext is IHasFocus)
                {
                    SetFocusedViewModel(((IHasFocus)_dataContext).CurrentlyFocused);
                }
            }
        }

        protected virtual void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_dataContext != null)
            {
                DataContext = _dataContext;
                if (_dataContext is IHasFocus)
                {
                    SetFocusedViewModel(((IHasFocus)_dataContext).CurrentlyFocused);
                }
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
        private bool _cancelForward = false;
        private CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();
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


        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            Window.Current.SizeChanged -= sizeChanged;
            if (e.NavigationMode == NavigationMode.Back || _cancelForward)
                _cancelTokenSource.Cancel();

        }

        string _stateGuid;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

            try
            {
                //_logger.Info("navigating to page " + GetType().Name);
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

                Window.Current.SizeChanged += sizeChanged;

                if (_stateGuid != null && (DataContext == null || e.NavigationMode == NavigationMode.New))
                {
                    //_logger.Info("loading state guid for page " + GetType().Name);
                    _dataContext = NavigationStateUtility.GetDataContext(_stateGuid);
                    if (_dataContext is IRefreshable)
                    {
                        ((IRefreshable)_dataContext).MaybeRefresh();
                    }
                    if (_dataContext is ICancellableViewModel)
                    {
                        _cancelForward = ((ICancellableViewModel)_dataContext).BindToken(_cancelTokenSource.Token);
                    }
                }

                if (DataContext != null && e.NavigationMode == NavigationMode.Back)
                {
                    if (DataContext is IHasFocus)
                    {
                        SetFocusedViewModel(((IHasFocus)DataContext).CurrentlyFocused);
                    }
                }

            }
            catch (Exception ex)
            {
                //_logger.Error("Failed navigating to page " + GetType().Name, ex);
            }
            base.OnNavigatedTo(e);
            //_logger.Info("finished loading page " + GetType().Name);
        }

        private async void sizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            ApplicationView currentView = ApplicationView.GetForCurrentView();
            await AdjustForOrientation(currentView.Orientation);
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
        public void PushNavState(object sender, string pushedState)
        {
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

        public virtual void SetFocusedViewModel(ViewModelBase viewModel)
        {

        }
    }

    public class PushNavState
    {
        private static T FindAncestor<T>(DependencyObject obj) where T : class
        {
            if (obj == null)
                return null;
            else if (obj is T)
                return obj as T;
            else
                return FindAncestor<T>(VisualTreeHelper.GetParent(obj));
        }

        public static void Execute(object sender, string parameter)
        {
            var appPage = FindAncestor<SnooApplicationPage>(sender as DependencyObject);
            if (appPage != null)
                appPage.PushNavState(sender, parameter);
        }
    }

}
