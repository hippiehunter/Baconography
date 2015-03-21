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
    public class UserActivityTemplateSelector : DependencyObject
    {
        public DataTemplate SingleActivityTemplate
        {
            get { return (DataTemplate)GetValue(SingleActivityProperty); }
            set { SetValue(SingleActivityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CommentActivitySingleTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SingleActivityProperty =
            DependencyProperty.Register("SingleActivityTemplate", typeof(DataTemplate), typeof(UserActivityTemplateSelector), new PropertyMetadata(null));

        public DataTemplate HeaderActivityTemplate
        {
            get { return (DataTemplate)GetValue(HeaderActivityProperty); }
            set { SetValue(HeaderActivityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderActivityProperty =
            DependencyProperty.Register("HeaderActivityTemplate", typeof(DataTemplate), typeof(UserActivityTemplateSelector), new PropertyMetadata(null));

        public DataTemplate BodyActivityTemplate
        {
            get { return (DataTemplate)GetValue(BodyActivityProperty); }
            set { SetValue(BodyActivityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BodyActivityProperty =
            DependencyProperty.Register("BodyActivityTemplate", typeof(DataTemplate), typeof(UserActivityTemplateSelector), new PropertyMetadata(null));



		public DataTemplateSelector Selector { get; set; }

		public UserActivityTemplateSelector()
		{
			Selector = new UserActivityTemplateSelectorImpl(this);
		}
	}
	class UserActivityTemplateSelectorImpl : DataTemplateSelector
	{
		private UserActivityTemplateSelector _selector;
		public UserActivityTemplateSelectorImpl(UserActivityTemplateSelector selector)
		{
			_selector = selector;
		}
		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            //decide if its a group or a single
            var group = item as ActivityGroupViewModel;
            if (group != null)
            {
                if (group.Activities.Count == 1)
                    return _selector.SingleActivityTemplate;
                else
                    return _selector.HeaderActivityTemplate;
            }
            var activity = item as ActivityViewModel;
            if(activity != null)
            {
                return _selector.BodyActivityTemplate;
            }
            else
                throw new ArgumentOutOfRangeException();
        }
    }
}
