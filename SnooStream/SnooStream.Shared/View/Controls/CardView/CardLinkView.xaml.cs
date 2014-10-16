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



		public bool PhasedLoad
		{
			get { return (bool)GetValue(PhasedLoadProperty); }
			set { SetValue(PhasedLoadProperty, value); }
		}

		// Using a DependencyProperty as the backing store for PhasedLoad.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty PhasedLoadProperty =
			DependencyProperty.Register("PhasedLoad", typeof(bool), typeof(CardLinkView), new PropertyMetadata(true, PhaseLoadChanged));

		private static void PhaseLoadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var thisp = d as CardLinkView;
			if (!thisp.PhasedLoad)
			{
				thisp.DataContextChanged += async (sender, args) =>
					{
						if (args.NewValue != null)
						{
							thisp.previewSection.Content = ContentPreviewConverter.MakePreviewControl(args.NewValue as LinkViewModel);
							var context = ((Preview)((UserControl)thisp.previewSection.Content).DataContext);
							var hqImageUrl = await Task.Run(() => context.FinishLoad(thisp.cancelSource.Token));
							if (string.IsNullOrWhiteSpace(hqImageUrl) || thisp.cancelSource.IsCancellationRequested)
								return;

							try
							{
								var previewUrl = await Task.Run(() => PlatformImageAcquisition.ImagePreviewFromUrl(hqImageUrl, thisp.cancelSource.Token));
								((Preview)((UserControl)thisp.previewSection.Content).DataContext).ThumbnailUrl = previewUrl;
							}
							catch (Exception ex)
							{
								SnooStreamViewModel.Logging.Log(ex);
							}
						}
					};
			}
		}

		internal async void PhaseLoad(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (args.InRecycleQueue)
			{
				cancelSource.Cancel();
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
						try
						{
							var context = ((Preview)((UserControl)previewSection.Content).DataContext);
							var hqImageUrl = await Task.Run(() => context.FinishLoad(cancelSource.Token));
							if (string.IsNullOrWhiteSpace(hqImageUrl) || cancelSource.IsCancellationRequested)
								return;

							try
							{
								var previewUrl = Task.Run(() => PlatformImageAcquisition.ImagePreviewFromUrl(hqImageUrl, cancelSource.Token));
								args.RegisterUpdateCallback(async (nestedSender, nestedArgs) =>
									{
										if (!nestedArgs.InRecycleQueue)
										{
											try
											{
												((Preview)((UserControl)previewSection.Content).DataContext).ThumbnailUrl = await previewUrl;
											}
											catch (OperationCanceledException)
											{
												//Do Nothing
											}
										}
									});
							}
							catch (OperationCanceledException)
							{
								//Do nothing
							}
						}
						catch (Exception ex)
						{
							SnooStreamViewModel.Logging.Log(ex);
						}
						break;
				}
			}
		}
	}
}
