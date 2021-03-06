﻿using SnooStream.Converters;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace SnooStream.View.Controls
{
    public sealed partial class CardCommentView : UserControl
    {
        public CardCommentView()
        {
            this.InitializeComponent();
#if WINDOWS_PHONE_APP
            Windows.UI.Xaml.Media.Animation.ContinuumNavigationTransitionInfo.SetIsEntranceElement(this, true);
#endif
        }

        private async void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (args.NewValue != null)
                contentSection.Content = await ContentPreviewConverter.MakePreviewControl(args.NewValue as LinkViewModel, SnooStreamViewModel.UIContextCancellationToken, null, true);
            else
                contentSection.Content = null;
        }
    }
}
