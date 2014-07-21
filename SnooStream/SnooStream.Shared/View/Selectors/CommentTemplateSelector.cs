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
    public class CommentTemplateSelector : DependencyObject
    {
		public CommentTemplateSelector()
		{
			Selector = new CommentTemplateSelectorImpl(this);
		}
        public DataTemplate CommentTemplate
        {
            get { return (DataTemplate)GetValue(CommentTemplateProperty); }
            set { SetValue(CommentTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommentTemplateProperty =
            DependencyProperty.Register("CommentTemplate", typeof(DataTemplate), typeof(CommentTemplateSelector), new PropertyMetadata(null));


        public DataTemplate MoreTemplate
        {
            get { return (DataTemplate)GetValue(MoreTemplateProperty); }
            set { SetValue(MoreTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MoreTemplateProperty =
            DependencyProperty.Register("MoreTemplate", typeof(DataTemplate), typeof(CommentTemplateSelector), new PropertyMetadata(null));

        public DataTemplate LoadFullyTemplate
        {
            get { return (DataTemplate)GetValue(LoadFullyTemplateProperty); }
            set { SetValue(LoadFullyTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LoadFullyTemplateProperty =
            DependencyProperty.Register("LoadFullyTemplate", typeof(DataTemplate), typeof(CommentTemplateSelector), new PropertyMetadata(null));


		public DataTemplateSelector Selector { get; set; }

		
    }
	class CommentTemplateSelectorImpl : DataTemplateSelector
	{
		private CommentTemplateSelector _selector;
		public CommentTemplateSelectorImpl(CommentTemplateSelector selector)
		{
			_selector = selector;
		}
		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			if (item is CommentViewModel)
				return _selector.CommentTemplate;
			else if (item is MoreViewModel)
				return _selector.MoreTemplate;
			else if (item is LoadFullCommentsViewModel)
				return _selector.LoadFullyTemplate;
			else throw new NotImplementedException();
		}
	}
}
