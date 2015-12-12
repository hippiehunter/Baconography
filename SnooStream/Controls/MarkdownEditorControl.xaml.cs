using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace SnooStream.Controls
{
    public sealed partial class MarkdownEditorControl : UserControl, INotifyPropertyChanged
    {
        public MarkdownEditorControl()
        {
            this.InitializeComponent();
        }

		private void TextBox_KeyUp(object sender, KeyRoutedEventArgs e)
		{
			BindingExpression bindingExpression = ((TextBox)sender).GetBindingExpression(TextBox.TextProperty);
			if (bindingExpression != null)
			{
				bindingExpression.UpdateSource();
			}
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			BindingExpression bindingExpression = ((TextBox)sender).GetBindingExpression(TextBox.TextProperty);
			if (bindingExpression != null)
			{
				bindingExpression.UpdateSource();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void Button_Tapped(object sender, TappedRoutedEventArgs e)
		{
			textBox.Focus(FocusState.Pointer);
			((ICommand)((ListViewItem)sender).DataContext).Execute(null);
			e.Handled = true;
		}
	}
}
