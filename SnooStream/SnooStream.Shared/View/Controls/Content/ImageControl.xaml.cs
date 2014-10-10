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
    public sealed partial class ImageControl : UserControl
    {
        public ImageControl()
        {
            this.InitializeComponent();
        }

		private bool imageSizeSet;
		private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (imageSizeSet || !(image.ActualWidth > scrollViewer.ViewportWidth) && !(image.ActualHeight > scrollViewer.ViewportHeight))
			{
				return;
			}

			// If the image is larger than the screen, zoom it out.
			var zoomFactor = (float)Math.Min(scrollViewer.ViewportWidth / image.ActualWidth, scrollViewer.ViewportHeight / image.ActualHeight);
			scrollViewer.ChangeView(null, null, zoomFactor, true);
			imageSizeSet = true;
		}
    }
}
