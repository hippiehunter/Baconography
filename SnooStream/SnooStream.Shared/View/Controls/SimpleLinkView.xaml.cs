using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using SnooStream.ViewModel;
using Windows.UI.Xaml.Controls;
using Windows.UI.Input;

namespace SnooStream.View.Controls
{
    public partial class SimpleLinkView : UserControl
    {
        public SimpleLinkView()
        {
            InitializeComponent();
        }

		private void Button_Tap(object sender, TappedEventArgs e)
        {
            ((LinkViewModel)DataContext).GotoLink.Execute(null);
        }
    }
}
