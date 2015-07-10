using SnooStream.ViewModel.Content;
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

namespace SnooStream.View.Controls.Content
{
    public sealed partial class AlbumControl : UserControl
    {
        public AlbumControl()
        {
            this.InitializeComponent();
        }

		private async void albumSlideView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var loader = albumSlideView.ItemsSource as ISupportIncrementalLoading;
			if (e.AddedItems.Count > 0 && albumSlideView.Items.Count < (albumSlideView.Items.IndexOf(e.AddedItems.First()) + 5) && loader != null)
			{
				if (loader.HasMoreItems)
					await loader.LoadMoreItemsAsync(20);
			}
            if (e.AddedItems.Count > 0)
                ((ContentViewModel)e.AddedItems[0]).Focused = true;

            if (e.RemovedItems.Count > 0)
                ((ContentViewModel)e.RemovedItems[0]).Focused = false;
		}

		private async void albumSlideView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			var loader = albumSlideView.ItemsSource as ISupportIncrementalLoading;
			if (albumSlideView.Items.Count == 0 && loader != null)
			{
				if (loader.HasMoreItems)
					await loader.LoadMoreItemsAsync(20);
			}
		}
    }
}
