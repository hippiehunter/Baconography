using Microsoft.Toolkit.Uwp.UI.Controls;
using SnooStream.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SnooStream.Common
{
    public class BladeBinder : DependencyObject
    {
        public static readonly DependencyProperty DataSourceProperty = DependencyProperty.RegisterAttached(
            "DataSource",
            typeof(object),
            typeof(BladeBinder), new PropertyMetadata(null, DataSourceChanged)
            );

        private static void DataSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var data = e.NewValue as HubNavGroup;
            var hub = d as BladeControl;
            if (data == null || hub == null) return;
            foreach (var hubItem in data.Sections)
            {
                hub.Items.Add(new BladeItem { DataContext = hubItem.Content, Width = 500, BorderThickness = new Thickness(0) , IsOpen = true, ContentTemplate = hubItem.ContentTemplate, Content = hubItem.Content, Title = hubItem.HeaderText, TitleBarVisibility = Visibility.Visible });
            }

            data.Sections.CollectionChanged += (obj, arg) =>
            {
                switch (arg.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        {
                            var hubItem = arg.NewItems[0] as HubNavItem;
                            var newBlade = new BladeItem { DataContext = hubItem.Content, BorderThickness = new Thickness(0), IsOpen = true, ContentTemplate = hubItem.ContentTemplate, Content = hubItem.Content, Title = hubItem.HeaderText, TitleBarVisibility = Visibility.Visible };
                            hub.Items.Add(newBlade);
                            //only show two items at a time
                            for (int i = 0; i < hub.Items.Count - 2; i++)
                            {
                                ((BladeItem)hub.Items[i]).IsOpen = false;
                            }

                            break;
                        }
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        {
                            var hubItem = arg.OldItems[0] as HubNavItem;
                            hub.Items.Remove(hub.Items.FirstOrDefault(section => ((BladeItem)section).DataContext == hubItem.Content));

                            for (int i = hub.Items.Count - 1; i > hub.Items.Count - 2; i--)
                            {
                                ((BladeItem)hub.Items[i]).IsOpen = true;
                            }

                            break;
                        }
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                        hub.Items.Clear();
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
}
