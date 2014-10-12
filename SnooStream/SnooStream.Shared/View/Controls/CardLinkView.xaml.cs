using SnooStream.Common;
using SnooStream.Converters;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
    public sealed partial class CardLinkView : UserControl
    {
        public CardLinkView()
        {
            this.InitializeComponent();
        }
		CancellationTokenSource cancelSource = new CancellationTokenSource();
		internal async void PhaseLoad(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (args.InRecycleQueue)
			{
				cancelSource.Cancel();
				previewSection.Content = null;
			}
			else
			{
				switch (args.Phase)
				{
					case 0:
						actionButtons.Opacity = 0;
						previewSection.Opacity = 0;
						linkMetadata.Opacity = 0;
						args.RegisterUpdateCallback(PhaseLoad);
						break;
					case 1:
						actionButtons.Opacity = 1;
						linkMetadata.Opacity = 1;
						args.RegisterUpdateCallback(PhaseLoad);
						break;
					case 2:
						previewSection.Opacity = 1;
						previewSection.Content = ContentPreviewConverter.MakePreviewControl(DataContext as LinkViewModel);
						args.RegisterUpdateCallback(PhaseLoad);
						break;
					case 3:
						var hqImageUrl = await ((Preview)((UserControl)previewSection.Content).DataContext).FinishLoad(cancelSource.Token);
						if (string.IsNullOrWhiteSpace(hqImageUrl) || cancelSource.IsCancellationRequested)
							return;

						try
						{
							var previewUrl = PlatformImageAcquisition.ImagePreviewFromUrl(hqImageUrl, cancelSource.Token);
							((Preview)((UserControl)previewSection.Content).DataContext).ThumbnailUrl = await previewUrl;
						}
						catch (OperationCanceledException)
						{
							//Do nothing
						}
						break;
				}
			}
		}
	}
}
