﻿using System;
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

namespace SnooStream.View.Controls
{
    public sealed partial class CardPreviewImageControl : UserControl
    {
        public CardPreviewImageControl()
        {
            this.InitializeComponent();
        }

        private void hqImageControl_Loaded(object sender, RoutedEventArgs e)
        {
            HQFadeIn.Begin();
        }

        private void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            hqImageControl.Opacity = 0;
            imageControl.Opacity = 1;
        }
    }
}
