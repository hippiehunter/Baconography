using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using SnooStream.ViewModel;
using Windows.UI.Xaml.Controls;
using Windows.UI.Input;
using Windows.UI.Xaml.Input;

namespace SnooStream.View.Controls
{
    public partial class SelfActivityView : UserControl
    {
        public SelfActivityView()
        {
            InitializeComponent();
        }

        private void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var activityGroup = (sender as Grid).DataContext as ActivityGroupViewModel;
            if (activityGroup != null)
            {
                activityGroup.Tapped();
            }
            else
            {
                var activity = (sender as Grid).DataContext as ActivityViewModel;
                if (activity != null)
                {
                    activity.Tapped();
                }
            }
            
        }
    }
}
