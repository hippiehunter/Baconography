using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using SnooStream.ViewModel;

namespace SnooStreamWP8.View.Controls
{
    public partial class SimpleLinkView : UserControl
    {
        public SimpleLinkView()
        {
            InitializeComponent();
        }

        private void Button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ((LinkViewModel)DataContext).GotoLink.Execute(null);
        }
    }
}
