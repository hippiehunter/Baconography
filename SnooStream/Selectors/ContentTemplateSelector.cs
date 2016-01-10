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
    class ContentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate AlbumViewTemplate { get; set; }
        public DataTemplate ImageContainerTemplate { get; set; }
        public DataTemplate PlainWebTemplate { get; set; }
        public DataTemplate PlainTextTemplate { get; set; }
        public DataTemplate VideoTemplate { get; set; }
        public DataTemplate CommentsViewTemplate { get; set; }
        public DataTemplate LoadingTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return SelectTemplateCore(item);
        }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is LoadViewModel)
                return LoadingTemplate;
            else if (item is ImageContentViewModel)
                return ImageContainerTemplate;
            else if (item is ContentContainerViewModel)
            {
                if (((ContentContainerViewModel)item).SingleViewItem)
                    return AlbumViewTemplate;
                else
                    return PlainWebTemplate;
            }
            else if (item is VideoContentViewModel)
                return VideoTemplate;
            else if (item is CommentsViewModel)
                return CommentsViewTemplate;
            else if (item is TextContentViewModel)
                return PlainTextTemplate;
            else
                return null;
        }
    }
}
