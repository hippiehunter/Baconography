using SnooStream.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SnooStream.View.Selectors
{
    public class PreviewTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextPreview { get; set; }
        public DataTemplate ImagePreview { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is PreviewText)
                return TextPreview;
            else
                return ImagePreview;
        }
    }
}
