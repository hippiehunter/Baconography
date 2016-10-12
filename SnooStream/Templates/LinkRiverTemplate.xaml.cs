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

namespace SnooStream.Templates
{
    public sealed partial class LinkRiverTemplate : ResourceDictionary
    {
        public LinkRiverTemplate()
        {
            this.InitializeComponent();
        }

        private void ListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            //nasty hack to prevent recycled items from getting misused by different data type content (that should have gotten selectored away
            if (args.InRecycleQueue)
            {
                try
                {
                    args.ItemContainer.DataContext = null;
                    args.ItemContainer.Content = null;
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
