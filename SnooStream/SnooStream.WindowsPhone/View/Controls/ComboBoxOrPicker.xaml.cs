using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace SnooStream.View.Controls
{
	public sealed partial class ComboBoxOrPicker : UserControl
	{
		public ComboBoxOrPicker()
		{
			this.InitializeComponent();
		}

		public object SelectedValue
		{
			get { return (object)GetValue(SelectedValueProperty); }
			set { SetValue(SelectedValueProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SelectedValue.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SelectedValueProperty =
			DependencyProperty.Register("SelectedValue", typeof(object), typeof(ComboBoxOrPicker), new PropertyMetadata(null));

		public object SelectedItem
		{
			get { return (object)GetValue(SelectedItemProperty); }
			set { SetValue(SelectedItemProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SelectedValue.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SelectedItemProperty =
			DependencyProperty.Register("SelectedItem", typeof(object), typeof(ComboBoxOrPicker), new PropertyMetadata(null));



		public IEnumerable<object> ItemsSource
		{
			get { return (IEnumerable<object>)GetValue(ItemsSourceProperty); }
			set { SetValue(ItemsSourceProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ItemsSource.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ItemsSourceProperty =
			DependencyProperty.Register("ItemsSource", typeof(IEnumerable<object>), typeof(ComboBoxOrPicker), new PropertyMetadata(Enumerable.Empty<object>()));


		public string Header
		{
			get { return (string)GetValue(HeaderProperty); }
			set { SetValue(HeaderProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Header.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty HeaderProperty =
			DependencyProperty.Register("Header", typeof(string), typeof(ComboBoxOrPicker), new PropertyMetadata(string.Empty));

	}
}
