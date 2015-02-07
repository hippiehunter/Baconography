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
using Windows.UI.Xaml.Media.Animation;
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

				if (previewSection != null)
				{
					if (previewSection.Content is CardPreviewImageControl)
					{
						var imageControl = previewSection.Content as CardPreviewImageControl;
                        if (imageControl.hqImageControl.Source is BitmapSource)
                        {
                            ((BitmapSource)imageControl.hqImageControl.Source).SetSource(_streamHack);
                        }
					}
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
                        var finishLoad2 = new Action(async () =>
                            {
                                try
                                {
                                    var cancelToken = cancelSource.Token;
                                    var previewControl = await ContentPreviewConverter.MakePreviewControl(DataContext as LinkViewModel, cancelToken, previewSection.Content);
                                    if (!cancelToken.IsCancellationRequested)
                                    {
                                        if (previewSection.Content != previewControl)
                                            previewSection.Content = previewControl;

                                        args.RegisterUpdateCallback((sender2, args2) => PhaseLoad(sender2, args2));
                                    }
                                }
                                catch (TaskCanceledException)
                                {
                                }
                            });
                        finishLoad2();
                        return false;
                    case 3:
                        try
                        {
                            //make sure its ok to load non essential content
                            if (SnooStreamViewModel.SystemServices.IsLowPriorityNetworkOk)
                            {
                                var cancelToken = cancelSource.Token;
                                var context = ((Preview)((UserControl)previewSection.Content).DataContext);
                                var finishLoad3 = new Action(async () =>
                                    {
                                        var hqImageUrl = await await SnooStreamViewModel.SystemServices.RunAsyncIdle(() => context.FinishLoad(cancelSource.Token), cancelToken);
                                        if (string.IsNullOrWhiteSpace(hqImageUrl) || cancelSource.IsCancellationRequested)
                                            return;

                                        try
                                        {
                                            if (previewSection.Content is CardPreviewImageControl)
                                            {
                                                var previewImage = previewSection.Content as CardPreviewImageControl;
                                                var previewUrl = await await SnooStreamViewModel.SystemServices.RunAsyncIdle(() => PlatformImageAcquisition.ImagePreviewFromUrl(hqImageUrl, cancelToken), cancelToken);
                                                var dataContext = ((Preview)((UserControl)previewSection.Content).DataContext);
                                                if (!cancelToken.IsCancellationRequested)
                                                {
                                                    dataContext.HQThumbnailUrl = previewUrl;
                                                }
                                            }
                                            CurrentLoadPhase++;
                                        }
                                        catch (TaskCanceledException)
                                        {
                                            //Do nothing
                                        }
                                    });
                                finishLoad3();
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

        private void Comments_Tapped(object sender, TappedRoutedEventArgs e)
        {
#if WINDOWS_PHONE_APP
            ContinuumNavigationTransitionInfo.SetIsExitElement(sender as UIElement, true);
#endif
        }
    }
}
