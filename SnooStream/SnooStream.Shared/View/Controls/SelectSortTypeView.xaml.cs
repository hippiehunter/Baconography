using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SnooStream.View.Controls
{
    public partial class SelectSortTypeView : UserControl
    {
        public SelectSortTypeView()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty SortOrderProperty =
            DependencyProperty.Register(
                "SortOrder",
                typeof(string),
                typeof(SelectSortTypeView),
                new PropertyMetadata("", OnSortOrderPropertyChanged)
            );

        public string SortOrder
        {
            get
            {
                return (string)GetValue(SortOrderProperty);
            }
            set
            {
                SetValue(SortOrderProperty, value);
                if (onCheckOrigin)
                    return;

                if (value.Contains("new"))
                    newRad.IsChecked = true;
                else if (value.Contains("top"))
                    topRad.IsChecked = true;
                else if (value.Contains("rising"))
                    risingRad.IsChecked = true;
                else if (value.Contains("controversial"))
                    controversialRad.IsChecked = true;
                else
                    hotRad.IsChecked = true;
            }
        }

        private static void OnSortOrderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (SelectSortTypeView)d;
            view.SortOrder = (string)e.NewValue;
        }

        bool onCheckOrigin = false;
        private void OnChecked(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;
            var content = button.Content as string;

            if (content != null)
            {
                onCheckOrigin = true;
                SortOrder = content;
                onCheckOrigin = false;
            }
        }
    }
}
