using GalaSoft.MvvmLight;
using SnooStream.Common;
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

        public override void SetFocusedViewModel(ViewModelBase viewModel)
        {
            base.SetFocusedViewModel(viewModel);
            this.flipView.SelectedItem = viewModel;
        }

		private async void flipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var focusContext = DataContext as IHasFocus;
            var linksContext = DataContext as IHasLinks;
			if (focusContext != null && linksContext != null)
			{
				if (e.AddedItems.Count > 0)
				{
                    focusContext.CurrentlyFocused = e.AddedItems.First() as ViewModelBase;
				}
				var loader = linksContext.Links as ISupportIncrementalLoading;
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
			if (itemContainer != null)
			{
				var itemContentControl = itemContainer.ContentTemplateRoot as T;
				if (itemContentControl != null)
				{
					return itemContentControl;
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

        private async void root_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (args.NewValue is IHasLinks)
            {
                var dataContext = args.NewValue as IHasLinks;
                flipView.ItemsSource = ContentStreamViewModel.MakeFilteredContentStream(dataContext.Links, dataContext.CurrentSelected);
                flipView.SelectedValue = dataContext.CurrentSelected;
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
    }
}
