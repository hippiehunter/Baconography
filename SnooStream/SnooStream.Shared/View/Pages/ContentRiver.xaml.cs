﻿using SnooStream.Common;
using SnooStream.View.Controls.Content;
using SnooStream.ViewModel;
using SnooStream.ViewModel.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SnooStream.View.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ContentRiver : SnooApplicationPage
    {
        public ContentRiver()
        {
            this.InitializeComponent();
        }

		protected override async void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			if(DataContext is IHasLinks)
			{
				var dataContext = DataContext as IHasLinks;
				flipView.ItemsSource = ContentStreamViewModel.MakeFilteredContentStream(dataContext.Links, dataContext.CurrentSelected);
				flipView.SelectedValue = dataContext.CurrentSelected;
				if (dataContext.CurrentSelected != null)
					SnooStreamViewModel.OfflineService.AddHistory(dataContext.CurrentSelected.Url);
				flipView.SelectionChanged += flipView_SelectionChanged;

                await Task.Delay(10);
                if (dataContext.CurrentSelected.Content is VideoViewModel)
                {
                    var videoControl = FindContentType<VideoControl>(flipView.ContainerFromItem(dataContext.CurrentSelected) as FlipViewItem);
                    if (videoControl != null)
                    {
                        videoControl.player.Position = new TimeSpan();
                        videoControl.player.Play();
                        videoControl.player.AutoPlay = true;
                    }
                }
            }
		}

		private async void flipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var dataContext = DataContext as IHasLinks;
			if (dataContext != null)
			{
				if (e.AddedItems.Count > 0)
				{
					dataContext.CurrentSelected = e.AddedItems.First() as ILinkViewModel;
					if (dataContext.CurrentSelected != null)
						SnooStreamViewModel.OfflineService.AddHistory(dataContext.CurrentSelected.Url);
				}
				var loader = dataContext.Links as ISupportIncrementalLoading;
				if (e.AddedItems.Count > 0 &&
					flipView.Items.Count < (flipView.Items.IndexOf(e.AddedItems.First()) + 5) &&
					loader != null)
				{
					if (loader.HasMoreItems)
						await loader.LoadMoreItemsAsync(20);
				}
			}

            try
            {
                if (e.RemovedItems.Count > 0)
                {
                    if (((ILinkViewModel)e.RemovedItems[0]).Content is VideoViewModel)
                    {
                        var videoControl = FindContentType<VideoControl>(flipView.ContainerFromItem(e.RemovedItems[0]) as FlipViewItem);
                        if (videoControl != null)
                        {
                            videoControl.player.Stop();
                            videoControl.player.Position = new TimeSpan();
                        }
                    }
                }

                if (e.AddedItems.Count > 0)
                {
                    if (((ILinkViewModel)e.AddedItems[0]).Content is VideoViewModel)
                    {
                        var videoControl = FindContentType<VideoControl>(flipView.ContainerFromItem(e.AddedItems[0]) as FlipViewItem);
                        if (videoControl != null)
                        {
                            videoControl.player.Position = new TimeSpan();
                            videoControl.player.Play();
                            videoControl.player.AutoPlay = true;
                        }
                    }
                }
            }
            catch { }
		}

        private T FindContentType<T>(FlipViewItem itemContainer) where T : class
        {
            var itemContentControl = itemContainer.ContentTemplateRoot as ContentControl;
            if (itemContentControl != null)
            {
                var nestedContentControl = itemContentControl.ContentTemplateRoot as T;
                if (nestedContentControl != null)
                {
                    return nestedContentControl as T;
                }
            }
            return null;
        }

		private bool _overlayVisible = true;
		private void Overlay_Click(object sender, RoutedEventArgs e)
		{
			_overlayVisible = !_overlayVisible;
			if (_overlayVisible) 
				fadeInOverlay.Begin();
			else 
				fadeOutOverlay.Begin();
		}

        private void caption_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }
    }
}
