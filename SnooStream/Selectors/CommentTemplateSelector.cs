using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SnooStream.Selectors
{
    public class CommentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate CommentTemplate { get; set; }
        public DataTemplate MoreTemplate { get; set; }
        public DataTemplate LoadFullyTemplate { get; set; }
        public DataTemplate LoadingTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is MoreViewModel)
                return MoreTemplate;
            else if (item is LoadViewModel)
                return LoadingTemplate;
            else if (item is LoadFullCommentsViewModel)
                return LoadFullyTemplate;
            else
                return CommentTemplate;
        }
    }
}
