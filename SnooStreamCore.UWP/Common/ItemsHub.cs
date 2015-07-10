using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SnooStream.Common
{
    public class ItemsHub : Hub
    {
		public IList ItemsSource
		{
			get { return (IList)GetValue(ItemsSourceProperty); }
			set { SetValue(ItemsSourceProperty, value); }
		}

		public static readonly DependencyProperty ItemsSourceProperty =
			DependencyProperty.Register("ItemsSource", typeof(IList), typeof(ItemsHub), new PropertyMetadata(null, ItemsSourceChanged));

		private static void ItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ItemsHub hub = d as ItemsHub;
			if (hub != null)
			{
				IList items = e.NewValue as IList;
				if (items != null)
				{
					hub.Sections.Clear();
					foreach (var item in items)
					{
						HubSection section = new HubSection();
						section.Header = item.ToString();
						var template = hub.Resources[item.ToString() + "Template"] as DataTemplate;
						section.ContentTemplate = template;
						hub.Sections.Add(section);
					}
				}
			}
		}
    }
}
