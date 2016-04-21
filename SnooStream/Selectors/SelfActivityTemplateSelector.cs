using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SnooStream.Selectors
{
    public class SelfActivityTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Header { get; set; }
        public DataTemplate Item { get; set; }
        public DataTemplate LoadItem { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is LoadViewModel)
                return LoadItem;
            else if (item is ActivityViewModel)
                return Item;
            else if (item is ActivityHeaderViewModel)
                return Header;

            Debug.Assert(false, "found invalid item selecting for Self Activity Template");
            return null;
        }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            return SelectTemplateCore(item, null);
        }
    }
}
