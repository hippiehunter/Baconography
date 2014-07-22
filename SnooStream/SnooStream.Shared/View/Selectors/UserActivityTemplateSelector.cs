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
        public DataTemplate CommentActivityTemplate
        {
            get { return (DataTemplate)GetValue(CommentActivityTemplateProperty); }
            set { SetValue(CommentActivityTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommentActivityTemplateProperty =
            DependencyProperty.Register("CommentActivityTemplate", typeof(DataTemplate), typeof(UserActivityTemplateSelector), new PropertyMetadata(null));

        public DataTemplate ModeratorActivityTemplate
        {
            get { return (DataTemplate)GetValue(ModeratorActivityTemplateProperty); }
            set { SetValue(ModeratorActivityTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModeratorActivityTemplateProperty =
            DependencyProperty.Register("ModeratorActivityTemplate", typeof(DataTemplate), typeof(UserActivityTemplateSelector), new PropertyMetadata(null));

        public DataTemplate MessageActivityTemplate
        {
            get { return (DataTemplate)GetValue(MessageActivityTemplateProperty); }
            set { SetValue(MessageActivityTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageActivityTemplateProperty =
            DependencyProperty.Register("MessageActivityTemplate", typeof(DataTemplate), typeof(UserActivityTemplateSelector), new PropertyMetadata(null));

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
            var firstActivity = group.FirstActivity;

            if (firstActivity is PostedCommentActivityViewModel || 
                firstActivity is RecivedCommentReplyActivityViewModel ||
                firstActivity is PostedLinkActivityViewModel)
            {
				return _selector.CommentActivityTemplate;
            }
            else if (firstActivity is ModeratorActivityViewModel)
            {
				return _selector.ModeratorActivityTemplate;
            }
            else if (firstActivity is ModeratorMessageActivityViewModel || firstActivity is MessageActivityViewModel)
            {
				return _selector.MessageActivityTemplate;
            }
            else
                throw new ArgumentOutOfRangeException();
        }
    }
}
