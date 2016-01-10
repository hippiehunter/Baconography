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
        public string HeaderText
        {
            get
            {
                if (Content is IHasTitle)
                    return ((IHasTitle)Content).Title;
                else
                    return "";
            }
        }
        public void Close()
        {
            Container.Remove(this);
        }
        public IEnumerable<IHubNavCommand> Commands
        {
            get
            {
                if (Content is IHasHubNavCommands)
                    return ((IHasHubNavCommands)Content).Commands;
                else
                    return Enumerable.Empty<IHubNavCommand>();
            }
        }
    }

    public interface IHasTitle
    {
        string Title { get; }
    }

    public interface IHasHubNavCommands
    {
        IEnumerable<IHubNavCommand> Commands { get; }
    }

    public interface IHubNavCommand
    {
        bool IsInput { get; }
        string Text { get; }
        string Glyph { get; }
        bool IsEnabled { get; }
        void Tapped();
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
            var data = e.NewValue as HubNavGroup;
            var hub = d as Hub;
            if (data == null || hub == null) return;
            foreach (var hubItem in data.Sections)
            {
                hub.Sections.Add(new HubSection { DataContext = hubItem.Content, ContentTemplate = hubItem.ContentTemplate, Header = hubItem.HeaderText, IsHeaderInteractive = false });
            }

            data.Sections.CollectionChanged += (obj, arg) =>
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

    public class AdaptiveHubNav : UserControl
    {
        bool? _isLarge;
        Frame _mainFrame;
        public List<HubNavItem> NavStack;
        public AdaptiveHubNav()
        {
            NavStack = new List<HubNavItem>();
            _mainFrame = new Frame();
            Content = _mainFrame;
            UpdateSize(Window.Current.Bounds.Width);
            Window.Current.SizeChanged += Current_SizeChanged;


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

        //private void AdaptiveHubNav_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        //{
        //    if (_navStack != args.NewValue)
        //    {
        //        _navStack = args.NewValue as List<HubNavItem>;
        //        if (_navStack != null)
        //        {
        //            foreach (var item in _navStack)
        //                item.Container = this;
        //        }

        //        _isLarge = null;
        //        UpdateSize(Window.Current.Bounds.Width);
        //    }
        //}

        public void Remove(HubNavItem item)
        {
            if (NavStack[NavStack.Count - 1] == item)
            {
                Back();
            }
            else
            {
                NavStack.Remove(item);
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
            return NavStack.Count > 1;
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
                        var currentItem = NavStack[NavStack.Count - 1];
                        if (currentItem.IsRoot)
                        {
                            _mainFrame.GoBack();
                        }
                        else
                        {
                            var navGroup = ((Page)_mainFrame.Content).DataContext as HubNavGroup;
                            if (navGroup == null)
                                _mainFrame.GoBack();
                            else
                                navGroup.Sections.RemoveAt(navGroup.Sections.Count - 1);
                        }
                        NavStack.Remove(currentItem);
                        return true;
                    }
                    else
                    {
                        _mainFrame.GoBack();
                        NavStack.RemoveAt(NavStack.Count - 1);
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
            if (NavStack.Count > 0 && NavStack.Last().Content == viewModel && NavStack.Last().ContentTemplate == template)
            {
                //Do nothing, we dont need to navigate to ourself
            }
            else
            {
                var hubNav = new HubNavItem { Content = viewModel, IsRoot = makeRoot, ContentTemplate = template, Container = this };
                NavStack.Add(hubNav);
                NavToHubItem(true, false, hubNav);
            }
        }

        private void NavToHubItem(bool doNavigate, bool insertCurrent, HubNavItem hubNav)
        {
            //if (_isLarge.Value)
            //{
            //    if (hubNav.IsRoot || !(((Page)_mainFrame.Content)?.DataContext is HubNavGroup))
            //    {
            //        var hubSections = new HubNavGroup { Sections = new ObservableCollection<HubNavItem> { hubNav } };
            //        if (doNavigate)
            //        {
            //            _mainFrame.Navigate(typeof(MultiPageHubView), hubSections);
            //        }
            //        else
            //        {
            //            _mainFrame.BackStack.Add(new PageStackEntry(typeof(MultiPageHubView), hubSections, null));
            //        }
            //    }
            //    else
            //    {
            //        var hubGroup = insertCurrent ? _mainFrame.BackStack.Last().Parameter as HubNavGroup : ((Page)_mainFrame.Content).DataContext as HubNavGroup;
            //        hubGroup.Sections.Add(hubNav);
            //    }
            //}
            //else
            {
                if (doNavigate)
                {
                    _mainFrame.Navigate(typeof(SinglePageHubView), hubNav);
                }
                else
                {
                    _mainFrame.BackStack.Add(new PageStackEntry(typeof(SinglePageHubView), hubNav, null));
                }
            }
        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            UpdateSize(e.Size.Width);
        }

        private void UpdateSize(double width)
        {
            if (NavStack == null)
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
            var lastNavItem = _isLarge.Value ? NavStack.LastOrDefault(item => item.IsRoot) : NavStack.LastOrDefault();
            var afterLastRoot = NavStack.Skip(NavStack.IndexOf(lastNavItem) + 1).ToArray();
            if (lastNavItem != null)
            {
                NavToHubItem(true, false, lastNavItem);
                foreach (var item in afterLastRoot)
                {
                    NavToHubItem(false, false, item);
                }
                foreach (var hubItem in NavStack)
                {
                    if (lastNavItem == hubItem)
                        break;

                    NavToHubItem(false, true, hubItem);
                }
            }
        }
    }
}
