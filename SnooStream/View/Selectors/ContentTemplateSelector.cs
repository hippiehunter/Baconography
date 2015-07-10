using SnooStream.View.Controls.Content;
using SnooStream.ViewModel;
using SnooStream.ViewModel.Content;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SnooStream.View.Selectors
{
	public class ContentTemplateSelector : DataTemplateSelector
    {
		public DataTemplate AlbumViewTemplate { get; set; }
		public DataTemplate ImageContainerTemplate { get; set; }
		public DataTemplate PlainWebTemplate { get; set; }
		public DataTemplate VideoTemplate { get; set; }
		public DataTemplate SelfViewTemplate { get; set; }
		public DataTemplate CommentsViewTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            var linkViewModel = item as ILinkViewModel;
            var content = linkViewModel.Content;
            content.StartLoad(SnooStreamViewModel.Settings.ContentTimeout);
            if (content is ImageViewModel)
                return ImageContainerTemplate;
            else if (content is AlbumViewModel)
                return AlbumViewTemplate;
            else if (content is VideoViewModel)
                return VideoTemplate;
            else if (content is PlainWebViewModel)
                return PlainWebTemplate;
            else if (content is SelfViewModel)
                return SelfViewTemplate;
            else
                return null;
        }
    }
}