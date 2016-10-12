using SnooStream.Common;
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

namespace SnooStream.Templates
{
    public sealed partial class CardLinkViewTemplate
    {
        public CardLinkViewTemplate()
        {
            this.InitializeComponent();
        }

        private void hqImageControl_Loaded(object sender, RoutedEventArgs e)
        {
            var parent = ((FrameworkElement)sender).Parent as FrameworkElement;
            if(parent != null)
                ((Image)parent.FindName("imageControl")).Opacity = 0;
        }

        private void Button_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (!(args.NewValue is ViewModel.LinkViewModel) && args.NewValue != null)
            {
                args.Handled = true;
                sender.DataContext = null;
            }
        }
    }
}
