using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SnooStream.View.Selectors
{
	public class ContentTemplateSelector : ContentControl
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



		public bool IsLoaded
		{
			get { return (bool)GetValue(IsLoadedProperty); }
			set { SetValue(IsLoadedProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsLoaded.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsLoadedProperty =
			DependencyProperty.Register("IsLoaded", typeof(bool), typeof(ContentTemplateSelector), new PropertyMetadata(false));

		

		public ContentTemplateSelector()
		{
			ContentTemplateSelector = new ContentTemplateSelectorImpl(this);
		}
	}
	class ContentTemplateSelectorImpl : DataTemplateSelector
	{
		private ContentTemplateSelector _selector;
		public ContentTemplateSelectorImpl(ContentTemplateSelector selector)
		{
			_selector = selector;
		}
		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
			if (item is LoadingContentViewModel)
				return _selector.LoadingContentTemplate;
			else if (item is InternalRedditContentViewModel)
				return _selector.InternalRedditContentTemplate;
			else if (item is SelfContentViewModel)
				return _selector.SelfContentTemplate;
			else if (item is AlbumViewModel)
				return _selector.AlbumContentTemplate;
			else if (item is ImageViewModel)
				return _selector.ImageContentTemplate;
			else if (item is VideoViewModel)
				return _selector.VideoContentTemplate;
			else if (item is ErrorContentViewModel)
				return _selector.ErrorContentTemplate;
			else if (item is WebViewModel)
			{
				if (((WebViewModel)item).NotText)
					return _selector.WebContentTemplate;
				else
					return _selector.TextWebContentTemplate;
			}
			else
				return null;
        }
    }
}
