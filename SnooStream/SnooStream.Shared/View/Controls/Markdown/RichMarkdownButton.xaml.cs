using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;
using SnooStream.ViewModel;
using GalaSoft.MvvmLight;

namespace SnooStreamWP8.View.Controls.Markdown
{
    public partial class RichMarkdownButton : Button
    {
        static SolidColorBrush history = new SolidColorBrush(Colors.Gray);
        static SolidColorBrush noHistory = new SolidColorBrush(Color.FromArgb(255, 218, 165, 32));

        public RichMarkdownButton(string url, object content)
        {
            InitializeComponent();
            this.BorderThickness = new Thickness(0);
            Url = url;
            RealContent = content as UIElement;
        }

        public static readonly DependencyProperty UrlProperty =
            DependencyProperty.Register(
                "Url",
                typeof(string),
                typeof(RichMarkdownButton),
                new PropertyMetadata(null)
            );

        public string Url
        {
            get { return (string)GetValue(UrlProperty); }
            set
            {
                if (SnooStreamViewModel.OfflineService.HasHistory(value))
                    this.Foreground = history;
                else
                    this.Foreground = noHistory;
                SetValue(UrlProperty, value);
            }
        }

        public static readonly DependencyProperty RealContentProperty =
            DependencyProperty.Register(
                "RealContent",
                typeof(UIElement),
                typeof(RichMarkdownButton),
                new PropertyMetadata(null)
            );

        public UIElement RealContent
        {
            get
            {
                return (UIElement)GetValue(RealContentProperty);
            }
            set
            {
                SetValue(RealContentProperty, value);
            }
        }

        protected override void OnClick()
        {
            SnooStreamViewModel.CommandDispatcher.GotoLink(DataContext as ViewModelBase, Url);
            base.OnClick();
        }
    }
}
