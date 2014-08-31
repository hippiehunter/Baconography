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
        ////////multi bodies
        //////public DataTemplate CommentActivityTemplate
        //////{
        //////    get { return (DataTemplate)GetValue(CommentActivityTemplateProperty); }
        //////    set { SetValue(CommentActivityTemplateProperty, value); }
        //////}

        //////// Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        //////public static readonly DependencyProperty CommentActivityTemplateProperty =
        //////    DependencyProperty.Register("CommentActivityTemplate", typeof(DataTemplate), typeof(UserActivityTemplateSelector), new PropertyMetadata(null));

        //////public DataTemplate ModeratorActivityTemplate
        //////{
        //////    get { return (DataTemplate)GetValue(ModeratorActivityTemplateProperty); }
        //////    set { SetValue(ModeratorActivityTemplateProperty, value); }
        //////}

        //////// Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        //////public static readonly DependencyProperty ModeratorActivityTemplateProperty =
        //////    DependencyProperty.Register("ModeratorActivityTemplate", typeof(DataTemplate), typeof(UserActivityTemplateSelector), new PropertyMetadata(null));

        //////public DataTemplate MessageActivityTemplate
        //////{
        //////    get { return (DataTemplate)GetValue(MessageActivityTemplateProperty); }
        //////    set { SetValue(MessageActivityTemplateProperty, value); }
        //////}

        //////// Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        //////public static readonly DependencyProperty MessageActivityTemplateProperty =
        //////    DependencyProperty.Register("MessageActivityTemplate", typeof(DataTemplate), typeof(UserActivityTemplateSelector), new PropertyMetadata(null));



        ////////Headers
        //////public DataTemplate CommentActivityHeaderTemplate
        //////{
        //////    get { return (DataTemplate)GetValue(CommentActivityHeaderTemplateProperty); }
        //////    set { SetValue(CommentActivityHeaderTemplateProperty, value); }
        //////}

        //////// Using a DependencyProperty as the backing store for CommentActivityHeaderTemplate.  This enables animation, styling, binding, etc...
        //////public static readonly DependencyProperty CommentActivityHeaderTemplateProperty =
        //////    DependencyProperty.Register("CommentActivityHeaderTemplate", typeof(DataTemplate), typeof(UserActivityTemplateSelector), new PropertyMetadata(null));

        //////public DataTemplate ModeratorActivityHeaderTemplate
        //////{
        //////    get { return (DataTemplate)GetValue(ModeratorActivityHeaderProperty); }
        //////    set { SetValue(ModeratorActivityHeaderProperty, value); }
        //////}

        //////// Using a DependencyProperty as the backing store for ModeratorActivityHeader.  This enables animation, styling, binding, etc...
        //////public static readonly DependencyProperty ModeratorActivityHeaderProperty =
        //////    DependencyProperty.Register("ModeratorActivityHeaderTemplate", typeof(DataTemplate), typeof(UserActivityTemplateSelector), new PropertyMetadata(null));

        //////public DataTemplate MessageActivityHeaderTemplate
        //////{
        //////    get { return (DataTemplate)GetValue(MessageActivityHeaderProperty); }
        //////    set { SetValue(MessageActivityHeaderProperty, value); }
        //////}

        //////// Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        //////public static readonly DependencyProperty MessageActivityHeaderProperty =
        //////    DependencyProperty.Register("MessageActivityHeaderTemplate", typeof(DataTemplate), typeof(UserActivityTemplateSelector), new PropertyMetadata(null));


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
