using SnooStream.ViewModel;
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
    public sealed partial class LoadItemTemplate : ResourceDictionary
    {
        public LoadItemTemplate()
        {
            this.InitializeComponent();
        }

        private async void Grid_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (sender.DataContext is LoadViewModel && args.NewValue != sender.DataContext)
            {
                ((LoadViewModel)sender.DataContext).Cancel();
            }

            if (args.NewValue is LoadViewModel)
            {
                await ((LoadViewModel)args.NewValue).LoadAsync();
            }
        }
    }
}
