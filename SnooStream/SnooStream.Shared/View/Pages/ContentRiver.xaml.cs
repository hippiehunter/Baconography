using SnooStream.Common;
using SnooStream.View.Controls.Content;
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

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			if(DataContext is LinkRiverViewModel)
			{
				var dataContext = DataContext as LinkRiverViewModel;
				flipView.ItemsSource = dataContext.Links;
				flipView.SelectedValue = dataContext.CurrentSelected;
				flipView.SelectionChanged += flipView_SelectionChanged;
			}
		}

		private async void flipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
            //if (e.RemovedItems.Count > 0)
            //{
            //    var flipIndex = flipView.Items.IndexOf(e.RemovedItems.First());
            //    if (flipIndex > 0)
            //    {
            //        var generatorPosition = flipView.ItemContainerGenerator.GeneratorPositionFromIndex(flipIndex);
            //        var container = flipView.ContainerFromIndex(flipIndex) as FlipViewItem;
            //        if (container != null)
            //        {
            //            var contentRoot = container.ContentTemplateRoot as ContentControl;
            //            contentRoot.DataContext = null;
            //        }
            //    }
            //}
			if (e.AddedItems.Count > 0 && DataContext is LinkRiverViewModel)
			{
				var dataContext = DataContext as LinkRiverViewModel;
				dataContext.CurrentSelected = e.AddedItems.First() as ILinkViewModel;
			}
			var loader = flipView.ItemsSource as ISupportIncrementalLoading;
			if (e.AddedItems.Count > 0 && 
				flipView.Items.Count < (flipView.Items.IndexOf(e.AddedItems.First()) + 5) &&
				loader != null)
			{
				if (loader.HasMoreItems)
					await loader.LoadMoreItemsAsync(20);
			}
		}
    }
}
