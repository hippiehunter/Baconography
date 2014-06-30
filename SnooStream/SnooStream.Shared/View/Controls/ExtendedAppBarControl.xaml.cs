using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Input;

namespace SnooStreamWP8.View.Controls
{
    public enum ExtendedAppMenuState
    {
        Extended,
        Collapsed
    }
    public partial class ExtendedAppBarControl : UserControl
    {
        public ExtendedAppBarControl()
        {
            InitializeComponent();
        }

        public double Opacity
        {
            get { return (double)GetValue(OpacityProperty); }
            set { SetValue(OpacityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Opacity.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OpacityProperty =
            DependencyProperty.Register("Opacity", typeof(double), typeof(ExtendedAppBarControl), new PropertyMetadata(0.66));

        

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(ExtendedAppBarControl), new PropertyMetadata(""));

        public string LastButtonSymbol
        {
            get { return (string)GetValue(LastButtonSymbolProperty); }
            set { SetValue(LastButtonSymbolProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LastButtonSymbol.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LastButtonSymbolProperty =
            DependencyProperty.Register("LastButtonSymbol", typeof(string), typeof(ExtendedAppBarControl), new PropertyMetadata(""));


        public ICommand LastButtonCommand
        {
            get { return (ICommand)GetValue(LastButtonCommandProperty); }
            set { SetValue(LastButtonCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LastButtonCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LastButtonCommandProperty =
            DependencyProperty.Register("LastButtonCommand", typeof(ICommand), typeof(ExtendedAppBarControl), new PropertyMetadata(null));



        public string LastButtonText
        {
            get { return (string)GetValue(LastButtonTextProperty); }
            set { SetValue(LastButtonTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LastButtonText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LastButtonTextProperty =
            DependencyProperty.Register("LastButtonText", typeof(string), typeof(ExtendedAppBarControl), new PropertyMetadata(""));

    }
}
