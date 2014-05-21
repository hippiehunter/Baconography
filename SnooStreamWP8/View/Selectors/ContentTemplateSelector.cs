using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Telerik.Windows.Controls;

namespace SnooStreamWP8.View.Selectors
{
    public class ContentTemplateSelector : DataTemplateSelectorControl
    {
        public DataTemplate SelfContentTemplate
        {
            get { return (DataTemplate)GetValue(SelfContentTemplateProperty); }
            set { SetValue(SelfContentTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelfContentTemplateProperty =
            DependencyProperty.Register("SelfContentTemplate", typeof(DataTemplate), typeof(ContentTemplateSelector), new PropertyMetadata(null));


        public DataTemplate AlbumContentTemplate
        {
            get { return (DataTemplate)GetValue(AlbumContentTemplateProperty); }
            set { SetValue(AlbumContentTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AlbumContentTemplateProperty =
            DependencyProperty.Register("AlbumContentTemplate", typeof(DataTemplate), typeof(ContentTemplateSelector), new PropertyMetadata(null));

        public DataTemplate ImageContentTemplate
        {
            get { return (DataTemplate)GetValue(ImageContentTemplateProperty); }
            set { SetValue(ImageContentTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImageContentTemplateProperty =
            DependencyProperty.Register("ImageContentTemplate", typeof(DataTemplate), typeof(ContentTemplateSelector), new PropertyMetadata(null));

        public DataTemplate ErrorContentTemplate
        {
            get { return (DataTemplate)GetValue(ErrorContentTemplateProperty); }
            set { SetValue(ErrorContentTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ErrorContentTemplateProperty =
            DependencyProperty.Register("ErrorContentTemplate", typeof(DataTemplate), typeof(ContentTemplateSelector), new PropertyMetadata(null));

        public DataTemplate LoadingContentTemplate
        {
            get { return (DataTemplate)GetValue(LoadingContentTemplateProperty); }
            set { SetValue(LoadingContentTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LoadingContentTemplateProperty =
            DependencyProperty.Register("LoadingContentTemplate", typeof(DataTemplate), typeof(ContentTemplateSelector), new PropertyMetadata(null));


        public DataTemplate VideoContentTemplate
        {
            get { return (DataTemplate)GetValue(VideoContentTemplateProperty); }
            set { SetValue(VideoContentTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VideoContentTemplateProperty =
            DependencyProperty.Register("VideoContentTemplate", typeof(DataTemplate), typeof(ContentTemplateSelector), new PropertyMetadata(null));

        public DataTemplate TextWebContentTemplate
        {
            get { return (DataTemplate)GetValue(TextWebContentTemplateProperty); }
            set { SetValue(TextWebContentTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextWebContentTemplateProperty =
            DependencyProperty.Register("TextWebContentTemplate", typeof(DataTemplate), typeof(ContentTemplateSelector), new PropertyMetadata(null));

        public DataTemplate WebContentTemplate
        {
            get { return (DataTemplate)GetValue(WebContentTemplateProperty); }
            set { SetValue(WebContentTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WebContentTemplateProperty =
            DependencyProperty.Register("WebContentTemplate", typeof(DataTemplate), typeof(ContentTemplateSelector), new PropertyMetadata(null));

        public DataTemplate InternalRedditContentTemplate
        {
            get { return (DataTemplate)GetValue(InternalRedditContentTemplateProperty); }
            set { SetValue(InternalRedditContentTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InternalRedditContentTemplateProperty =
            DependencyProperty.Register("InternalRedditContentTemplate", typeof(DataTemplate), typeof(ContentTemplateSelector), new PropertyMetadata(null));

		public DataTemplate GifTemplate
		{
			get { return (DataTemplate)GetValue(GifTemplateProperty); }
			set { SetValue(GifTemplateProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty GifTemplateProperty =
			DependencyProperty.Register("GifTemplate", typeof(DataTemplate), typeof(ContentTemplateSelector), new PropertyMetadata(null));

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is ContentViewModel)
            {
                ((ContentViewModel)item).BeginLoad(true);
            }

            if (item is LoadingContentViewModel)
                return LoadingContentTemplate;
            else if (item is InternalRedditContentViewModel)
                return InternalRedditContentTemplate;
            else if (item is SelfContentViewModel)
                return SelfContentTemplate;
            else if (item is AlbumViewModel)
                return AlbumContentTemplate;
			else if(item is ImageViewModel)
			{
				var image = item as ImageViewModel;
				return image.IsGif ? GifTemplate : ImageContentTemplate;
			}
			else if(item is VideoViewModel)
				return VideoContentTemplate;
			else if(item is ErrorContentViewModel)
				return ErrorContentTemplate;
			else if(item is WebViewModel)
			{
				if(((WebViewModel)item).NotText)
					return WebContentTemplate;
				else
					return TextWebContentTemplate;
			}
			else
				throw new ArgumentOutOfRangeException();
        }
    }
}
