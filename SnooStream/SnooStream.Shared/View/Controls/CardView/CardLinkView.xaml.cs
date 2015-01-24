using SnooStream.Common;
using SnooStream.Converters;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
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
		private static IRandomAccessStream _streamHack;
		static CardLinkView()
		{
			var memStream = new MemoryStream();
			memStream.WriteByte(0);
			_streamHack  = memStream.AsRandomAccessStream();
		}

        private int CurrentLoadPhase = 0;

		internal bool PhaseLoad(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (args.InRecycleQueue || (args.Phase == 0 && CurrentLoadPhase != 0))
			{
                CurrentLoadPhase = 0;
				cancelSource.Cancel();
                cancelSource = new CancellationTokenSource();
                rootGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
				if (previewSection != null)
				{
					if (previewSection.Content is CardPreviewImageControl)
					{
						var imageControl = previewSection.Content as CardPreviewImageControl;
						if (imageControl.imageControl.Source is BitmapSource)
						{
							((BitmapSource)imageControl.imageControl.Source).SetSource(_streamHack);
						}
					}
					previewSection.Content = null;
				}
			}

            if (!args.InRecycleQueue)
            {
                switch (CurrentLoadPhase)
                {
                    case 0:
                        CurrentLoadPhase++;
                        return true;
                    case 1:
                        CurrentLoadPhase++;
                        rootGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        return true;
                    case 2:
                        CurrentLoadPhase++;
                        previewSection.Content = ContentPreviewConverter.MakePreviewControl(DataContext as LinkViewModel);
                        return true;
                    case 3:
                        try
                        {
                            //make sure its ok to load non essential content
                            if (SnooStreamViewModel.SystemServices.IsLowPriorityNetworkOk)
                            {
                                var context = ((Preview)((UserControl)previewSection.Content).DataContext);
                                var finishLoad = new Action(async () =>
                                    {
                                        var hqImageUrl = await Task.Run(() => context.FinishLoad(cancelSource.Token));
                                        if (string.IsNullOrWhiteSpace(hqImageUrl) || cancelSource.IsCancellationRequested)
                                            return;

                                        try
                                        {
                                            var cancelToken = cancelSource.Token;
                                            var previewUrl = await Task.Run(() => PlatformImageAcquisition.ImagePreviewFromUrl(hqImageUrl, cancelToken));
                                            if (!cancelSource.IsCancellationRequested)
                                            {
                                                ((Preview)((UserControl)previewSection.Content).DataContext).ThumbnailUrl = previewUrl;
                                                CurrentLoadPhase++;
                                            }
                                        }
                                        catch (OperationCanceledException)
                                        {
                                            //Do nothing
                                        }
                                    });
                                finishLoad();
                            }
                        }
                        catch (Exception ex)
                        {
                            SnooStreamViewModel.Logging.Log(ex);
                        }
                        break;
                }
            }
            return false;
		}
    }
}
