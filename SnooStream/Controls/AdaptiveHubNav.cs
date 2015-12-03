using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SnooStream.Controls
{
    public class HubNavItem
    {
        public AdaptiveHubNav Container { get; set; }
        public bool IsRoot { get; set; }
        public DataTemplate ContentTemplate { get; set; }
        public object Content { get; set; }
        public string HeaderText { get; } = "Header";
        public void Close()
        {
            Container.Remove(this);
        }
    }

    public class HubNavGroup
    {
        public ObservableCollection<HubNavItem> Sections { get; set; }
    }

    public class HubBinder : DependencyObject
    {
        public static readonly DependencyProperty DataSourceProperty = DependencyProperty.RegisterAttached(
            "DataSource",
            typeof(object),
            typeof(HubBinder), new PropertyMetadata(null, DataSourceChanged)
            );

        private static void DataSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var data = e.NewValue as ObservableCollection<HubNavItem>;
            var hub = d as Hub;
            if (data == null || hub == null) return;
            foreach (var hubItem in data)
            {
                hub.Sections.Add(new HubSection { DataContext = hubItem.Content, ContentTemplate = hubItem.ContentTemplate, Header = hubItem.HeaderText, IsHeaderInteractive = false });
            }

            data.CollectionChanged += (obj, arg) =>
            {
                switch (arg.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        {
                            var hubItem = arg.NewItems[0] as HubNavItem;
                            hub.Sections.Add(new HubSection { DataContext = hubItem.Content, ContentTemplate = hubItem.ContentTemplate, Header = hubItem.HeaderText, IsHeaderInteractive = false } );
                            break;
                        }
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        {
                            var hubItem = arg.OldItems[0] as HubNavItem;
                            hub.Sections.Remove(hub.Sections.FirstOrDefault(section => section.DataContext == hubItem.Content));
                            break;
                        }
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                        hub.Sections.Clear();
                        break;
                    default:
                        break;
                }
            };
        }

        public static void SetDataSource(UIElement element, object value)
        {
            element.SetValue(DataSourceProperty, value);
        }

        public static object GetDataSource(UIElement element)
        {
            return element.GetValue(DataSourceProperty);
        }
    }

    partial class MultiPageHubView : Page { }
    partial class SinglePageNavView : Page { }

    public class AdaptiveHubNav : UserControl
    {
        bool? _isLarge;
        Frame _mainFrame;
        List<HubNavItem> _navStack;
        public AdaptiveHubNav()
        {
            _navStack = new List<HubNavItem>();
            _mainFrame = new Frame();
            Content = _mainFrame;
            UpdateSize(Window.Current.Bounds.Width);
            Window.Current.SizeChanged += Current_SizeChanged;
            DataContextChanged += AdaptiveHubNav_DataContextChanged;


            if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                Windows.Phone.UI.Input.HardwareButtons.BackPressed += (s, a) =>
                {
                    a.Handled = Back();
                };
            }
            else
            {
                Windows.UI.Core.SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += (s, a) =>
                {
                    a.Handled = Back();
                };
            }
        }

        private void AdaptiveHubNav_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (_navStack != args.NewValue)
            {
                _navStack = args.NewValue as List<HubNavItem>;
                if (_navStack != null)
                {
                    foreach (var item in _navStack)
                        item.Container = this;
                }

                _isLarge = null;
                UpdateSize(Window.Current.Bounds.Width);
            }
        }

        public void Remove(HubNavItem item)
        {
            if (_navStack[_navStack.Count - 1] == item)
            {
                Back();
            }
            else
            {
                _navStack.Remove(item);
                var foundStackItem = _mainFrame.BackStack.FirstOrDefault(pse =>
                {
                    if (pse.Parameter is HubNavGroup)
                    {
                        var foundNavGroup = pse.Parameter as HubNavGroup;
                        return foundNavGroup.Sections.Any(section =>
                        {
                            return section.Content == item;
                        });
                    }
                    else if (pse.Parameter == item)
                    {
                        return true;
                    }
                    return false;
                });

                var navGroup = foundStackItem?.Parameter as HubNavGroup ??
                    ((Page)_mainFrame.Content).DataContext as HubNavGroup;

                if (navGroup != null)
                {
                    var foundSection = navGroup.Sections.FirstOrDefault(section => section.Content == item);
                    if (foundSection != null)
                    {
                        navGroup.Sections.Remove(foundSection);
                    }

                    if (navGroup.Sections.Count == 0)
                    {
                        if (!_mainFrame.BackStack.Remove(foundStackItem))
                            _mainFrame.GoBack();
                    }
                    else
                    {
                        ((HubNavItem)navGroup.Sections.FirstOrDefault().Content).IsRoot = true;
                    }
                }
                else
                {
                    _mainFrame.BackStack.Remove(foundStackItem);
                }
            }

        }

        public bool CanGoBack()
        {
            return _navStack.Count > 1;
        }

        public bool Back()
        {
            if (!CanGoBack())
                return false;
            else
            {
                if (_isLarge != null)
                {
                    if (_isLarge.Value)
                    {
                        var currentItem = _navStack[_navStack.Count - 1];
                        if (currentItem.IsRoot)
                        {
                            _mainFrame.GoBack();
                        }
                        else
                        {
                            var navGroup = ((Page)_mainFrame.Content).DataContext as HubNavGroup;
                            navGroup.Sections.RemoveAt(navGroup.Sections.Count - 1);
                        }
                        _navStack.Remove(currentItem);
                        return true;
                    }
                    else
                    {
                        _mainFrame.GoBack();
                        _navStack.RemoveAt(_navStack.Count - 1);
                        return true;
                    }
                }
                else
                    return false;
            }
        }

        public DataTemplate HubItemHeaderTemplate { get; set; }

        public void Navigate(object viewModel, DataTemplate template, bool makeRoot)
        {
            if (_navStack.Count > 0 && _navStack.Last().Content == viewModel && _navStack.Last().ContentTemplate == template)
            {
                //Do nothing, we dont need to navigate to ourself
            }
            else
            {
                var hubNav = new HubNavItem { Content = viewModel, IsRoot = makeRoot, ContentTemplate = template, Container = this };
                _navStack.Add(hubNav);
                NavToHubItem(true, false, hubNav);
            }
        }

        private void NavToHubItem(bool doNavigate, bool insertCurrent, HubNavItem hubNav)
        {
            if (_isLarge.Value)
            {
                var hubSection = new HubSection { DataContext = hubNav.Content, ContentTemplate = hubNav.ContentTemplate, HeaderTemplate = HubItemHeaderTemplate, IsHeaderInteractive = false, Header = hubNav.HeaderText, };
                if (hubNav.IsRoot || !(((Page)_mainFrame.Content)?.DataContext is HubNavGroup))
                {
                    var hubSections = new HubNavGroup { Sections = new ObservableCollection<HubNavItem> { hubNav } };
                    if (doNavigate)
                    {
                        _mainFrame.Navigate(typeof(MultiPageHubView), hubSections);
                    }
                    else
                    {
                        _mainFrame.BackStack.Add(new PageStackEntry(typeof(MultiPageHubView), hubSections, null));
                    }
                }
                else
                {
                    var hubGroup = insertCurrent ? _mainFrame.BackStack.Last().Parameter as HubNavGroup : ((Page)_mainFrame.Content).DataContext as HubNavGroup;
                    hubGroup.Sections.Add(hubNav);
                }
            }
            else
            {
                if (doNavigate)
                {
                    _mainFrame.Navigate(typeof(SinglePageNavView), hubNav);
                }
                else
                {
                    _mainFrame.BackStack.Add(new PageStackEntry(typeof(SinglePageNavView), hubNav, null));
                }
            }
        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            UpdateSize(e.Size.Width);
        }

        private void UpdateSize(double width)
        {
            if (_navStack == null)
                return;

            if (width > 500)
            {
                if (_isLarge == null || !_isLarge.Value)
                {
                    _isLarge = true;
                    SetNavStack();
                }
            }
            else
            {
                if (_isLarge == null || _isLarge.Value)
                {
                    _isLarge = false;
                    SetNavStack();
                }
            }
        }

        private void SetNavStack()
        {
            var lastNavItem = _isLarge.Value ? _navStack.LastOrDefault(item => item.IsRoot) : _navStack.LastOrDefault();
            var afterLastRoot = _navStack.Skip(_navStack.IndexOf(lastNavItem) + 1).ToArray();
            if (lastNavItem != null)
            {
                NavToHubItem(true, false, lastNavItem);
                foreach (var item in afterLastRoot)
                {
                    NavToHubItem(false, false, item);
                }
                foreach (var hubItem in _navStack)
                {
                    if (lastNavItem == hubItem)
                        break;

                    NavToHubItem(false, true, hubItem);
                }
            }
        }
    }
}
