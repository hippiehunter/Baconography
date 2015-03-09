using SnooStream.View.Controls.Content;
using SnooStream.ViewModel;
using SnooStream.ViewModel.Content;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SnooStream.View.Selectors
{
	public class ContentTemplateSelector : DependencyObject
	{
		public ContentTemplateSelector()
		{
			Selector = new ContentTemplateSelectorImpl(this);
		}

		public DataTemplate AlbumViewTemplate
		{
			get { return (DataTemplate)GetValue(AlbumViewTemplateProperty); }
			set { SetValue(AlbumViewTemplateProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty AlbumViewTemplateProperty =
			DependencyProperty.Register("AlbumViewTemplate", typeof(DataTemplate), typeof(ContentTemplateSelector), new PropertyMetadata(null));


		public DataTemplate ImageContainerTemplate
		{
			get { return (DataTemplate)GetValue(ImageContainerTemplateProperty); }
			set { SetValue(ImageContainerTemplateProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ImageContainerTemplateProperty =
			DependencyProperty.Register("ImageContainerTemplate", typeof(DataTemplate), typeof(ContentTemplateSelector), new PropertyMetadata(null));

		public DataTemplate PlainWebTemplate
		{
			get { return (DataTemplate)GetValue(PlainWebTemplateProperty); }
			set { SetValue(PlainWebTemplateProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty PlainWebTemplateProperty =
			DependencyProperty.Register("PlainWebTemplate", typeof(DataTemplate), typeof(ContentTemplateSelector), new PropertyMetadata(null));

		public DataTemplate VideoTemplate
		{
			get { return (DataTemplate)GetValue(VideoTemplateProperty); }
			set { SetValue(VideoTemplateProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty VideoTemplateProperty =
			DependencyProperty.Register("VideoTemplate", typeof(DataTemplate), typeof(ContentTemplateSelector), new PropertyMetadata(null));

		public DataTemplate SelfViewTemplate
		{
			get { return (DataTemplate)GetValue(SelfViewTemplateProperty); }
			set { SetValue(SelfViewTemplateProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SelfViewTemplateProperty =
			DependencyProperty.Register("SelfViewTemplate", typeof(DataTemplate), typeof(ContentTemplateSelector), new PropertyMetadata(null));

		public DataTemplate CommentsViewTemplate
		{
			get { return (DataTemplate)GetValue(CommentsViewTemplateProperty); }
			set { SetValue(CommentsViewTemplateProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CommentsViewTemplateProperty =
			DependencyProperty.Register("CommentsViewTemplate", typeof(DataTemplate), typeof(ContentTemplateSelector), new PropertyMetadata(null));


		public DataTemplateSelector Selector { get; set; }
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
			var linkViewModel = item as ILinkViewModel;
			var content = linkViewModel.Content;
			content.StartLoad(SnooStreamViewModel.Settings.ContentTimeout);
			if (content is ImageViewModel)
				return _selector.ImageContainerTemplate;
			else if (content is AlbumViewModel)
				return _selector.AlbumViewTemplate;
			else if (content is VideoViewModel)
				return _selector.VideoTemplate;
			else if (content is PlainWebViewModel)
				return _selector.PlainWebTemplate;
			else if (content is SelfViewModel)
				return _selector.SelfViewTemplate;
			else
				return null;
		}
	}
}