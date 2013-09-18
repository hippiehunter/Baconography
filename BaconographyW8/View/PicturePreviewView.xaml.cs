﻿using BaconographyPortable.ViewModel;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace BaconographyW8.View
{
    public sealed partial class PicturePreviewView : UserControl
    {
        public PicturePreviewView()
        {
            this.InitializeComponent();
        }

		private void FlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var dataContext = this.DataContext as BaconographyW8.Converters.PreviewDataConverter.PreviewImageViewModelWrapper;
			if (dataContext != null && flipView.SelectedIndex >= 0)
			{
				dataContext.CurrentPosition = flipView.SelectedIndex;
			}
		}
    }
}
