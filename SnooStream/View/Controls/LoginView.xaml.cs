using SnooStream.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using Windows.UI.Xaml.Controls;

namespace SnooStream.View.Controls
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

		private void StoredCredential_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
		{
			var textBlock = sender as TextBlock;
			var userCredential = textBlock.DataContext as UserCredential;
		}
    }
}
