using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;

namespace SnooStreamWP8.View.Selectors
{
    public class UserActivityTemplateSelector : DataTemplateSelector
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

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            //decide if its a group or a single
            var group = item as ActivityGroupViewModel;
            var firstActivity = group.FirstActivity;

            if (firstActivity is PostedCommentActivityViewModel || 
                firstActivity is RecivedCommentReplyActivityViewModel ||
                firstActivity is PostedLinkActivityViewModel)
            {
                return CommentActivityTemplate;
            }
            else if (firstActivity is ModeratorActivityViewModel)
            {
                return ModeratorActivityTemplate;
            }
            else if (firstActivity is ModeratorMessageActivityViewModel || firstActivity is MessageActivityViewModel)
            {
                return MessageActivityTemplate;
            }
            else
                throw new ArgumentOutOfRangeException();
        }
    }
}
