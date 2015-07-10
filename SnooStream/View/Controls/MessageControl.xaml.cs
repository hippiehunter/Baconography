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
    public sealed partial class MessageControl : UserControl
    {
        public MessageControl()
        {
            this.InitializeComponent();
        }

        internal void PhaseLoad(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (!args.InRecycleQueue)
            {
                switch (args.Phase)
                {
                    case 0:
                        {
                            plainTextControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                            markdownControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                            markdownControl.Markdown = null;
                            args.Handled = true;
                            args.RegisterUpdateCallback(PhaseLoad);
                            break;
                        }
                    case 1:
                        {
                            plainTextControl.Visibility = Windows.UI.Xaml.Visibility.Visible;
                            if (args.Item is MessageActivityViewModel)
                                plainTextControl.Text = ((MessageActivityViewModel)args.Item).Body;

                            args.Handled = true;
                            args.RegisterUpdateCallback(PhaseLoad);
                            break;
                        }
                    case 2:
                        {
                            if (args.Item is MessageActivityViewModel)
                            {
                                var body = ((MessageActivityViewModel)args.Item).Body;
                                args.Handled = true;
                                var markdownBody = SnooStreamViewModel.MarkdownProcessor.Process(body);

                                if (!SnooStreamViewModel.MarkdownProcessor.IsPlainText(markdownBody))
                                {
                                    plainTextControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                                    plainTextControl.Text = "";

                                    markdownControl.Visibility = Windows.UI.Xaml.Visibility.Visible;
                                    markdownControl.Markdown = markdownBody.MarkdownDom as SnooDom.SnooDom;
                                }
                            }
                            break;
                        }
                }
            }
            else
            {
            }
        }
    }
}
