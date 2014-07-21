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
    public class ImageTemplateSelector : DependencyObject
    {
        public DataTemplate GifTemplate
        {
            get { return (DataTemplate)GetValue(GifTemplateProperty); }
            set { SetValue(GifTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GifTemplateProperty =
            DependencyProperty.Register("GifTemplate", typeof(DataTemplate), typeof(ImageTemplateSelector), new PropertyMetadata(null));


        public DataTemplate StaticTemplate
        {
            get { return (DataTemplate)GetValue(StaticTemplateProperty); }
            set { SetValue(StaticTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StaticTemplateProperty =
            DependencyProperty.Register("StaticTemplate", typeof(DataTemplate), typeof(ImageTemplateSelector), new PropertyMetadata(null));

		public DataTemplateSelector Selector { get; set; }

		public ImageTemplateSelector()
		{
			Selector = new ImageTemplateSelectorImpl(this);
		}
	}
	class ImageTemplateSelectorImpl : DataTemplateSelector
	{
		private ImageTemplateSelector _selector;
		public ImageTemplateSelectorImpl(ImageTemplateSelector selector)
		{
			_selector = selector;
		}
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var image = item as ImageViewModel;
            if (image != null)
            {
				return image.IsGif ? _selector.GifTemplate : _selector.StaticTemplate;
            }
            else
                return null;
        }
    }
}
