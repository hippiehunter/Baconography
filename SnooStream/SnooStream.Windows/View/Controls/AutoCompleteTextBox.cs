using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SnooStream.View.Controls
{
    public class AutoCompleteTextBox : TextBox
    {
        public AutoCompleteTextBox()
        {
            //TODO fill me in
        }

        public object ItemsSource
        {
            get { return (object)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemsSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(object), typeof(AutoCompleteTextBox), new PropertyMetadata(null));

        public RelayCommand<string> ItemSelected
        {
            get { return (RelayCommand<string>)GetValue(ItemSelectedProperty); }
            set { SetValue(ItemSelectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemSelected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemSelectedProperty =
            DependencyProperty.Register("ItemSelected", typeof(RelayCommand<string>), typeof(AutoCompleteTextBox), new PropertyMetadata(null));

        public Style TextBoxStyle
        {
            get { return (Style)GetValue(TextBoxStyleProperty); }
            set { SetValue(TextBoxStyleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TextBoxStyle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextBoxStyleProperty =
            DependencyProperty.Register("TextBoxStyle", typeof(Style), typeof(AutoCompleteTextBox), new PropertyMetadata(null, OnStyleSet));

        private static void OnStyleSet(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TextBox)d).Style = e.NewValue as Style;
        }
    }
}
