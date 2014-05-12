using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using Telerik.Windows.Controls.SlideView;
using SnooStreamWP8.Common;

namespace SnooStreamWP8.View.Controls
{
    public partial class AlbumControl : UserControl
    {
        public AlbumControl()
        {
            InitializeComponent();
        }

        public ManipulationController ManipulationController
        {
            get { return (ManipulationController)GetValue(ManipulationControllerProperty); }
            set { SetValue(ManipulationControllerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ManipulationController.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ManipulationControllerProperty =
            DependencyProperty.Register("ManipulationController", typeof(ManipulationController), typeof(AlbumControl), new PropertyMetadata(null));

        private void PanAndZoomImage_Unloaded(object sender, RoutedEventArgs e)
        {
            //image controls leak 100% of their memory if you dont explicitly clear the UriSource on them when they are detached from the visual hierarchy
            var pZoom = sender as PanAndZoomImage;
            if (pZoom.Source is BitmapImage)
            {
                ((BitmapImage)pZoom.Source).UriSource = null;
            }
            pZoom.Source = null;
        }

        private void GifControl_Unloaded(object sender, RoutedEventArgs e)
        {
            //gif control also leaks if you dont clear its imagesource and manipulationController
            var gControl = sender as GifControl;
            gControl.ImageSource = null;
            gControl.ManipulationController = null;
        }
    }
}
