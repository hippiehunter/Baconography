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
    public class SearchResultTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Link { get; set; }
        public DataTemplate Subreddit { get; set; }
        public DataTemplate LoadItem { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is LoadViewModel)
                return LoadItem;
            else if (item is LinkViewModel)
                return Link;
            else if (item is SearchSubredditViewModel)
                return Subreddit;

            Debug.Assert(false, "found invalid item selecting for Search Template");
            return null;
        }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            return SelectTemplateCore(item, null);
        }
    }
}
