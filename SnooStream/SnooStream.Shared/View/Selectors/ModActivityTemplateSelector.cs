using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Linq;

namespace SnooStream.View.Selectors
{
    public class ModActivityTemplateSelector : DependencyObject
    {
        public DataTemplate QueueActivityTemplate
        {
            get { return (DataTemplate)GetValue(QueueActivityTemplateProperty); }
            set { SetValue(QueueActivityTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for QueueActivityTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty QueueActivityTemplateProperty =
            DependencyProperty.Register("QueueActivityTemplate", typeof(DataTemplate), typeof(ModActivityTemplateSelector), new PropertyMetadata(null));
        
        public DataTemplate ReportActivityTemplate
        {
            get { return (DataTemplate)GetValue(ReportActivityTemplateProperty); }
            set { SetValue(ReportActivityTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ReportActivityTemplateProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ReportActivityTemplateProperty =
            DependencyProperty.Register("ReportActivityTemplate", typeof(DataTemplate), typeof(ModActivityTemplateSelector), new PropertyMetadata(null));


        public DataTemplate ReportHeaderActivityTemplate
        {
            get { return (DataTemplate)GetValue(ReportHeaderActivityTemplateProperty); }
            set { SetValue(ReportHeaderActivityTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ReportActivityTemplateProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ReportHeaderActivityTemplateProperty =
            DependencyProperty.Register("ReportHeaderActivityTemplate", typeof(DataTemplate), typeof(ModActivityTemplateSelector), new PropertyMetadata(null));



        public DataTemplate SingleMessageActivityTemplate
        {
            get { return (DataTemplate)GetValue(SingleMessageActivityProperty); }
            set { SetValue(SingleMessageActivityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SingleMessageActivityTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SingleMessageActivityProperty =
            DependencyProperty.Register("SingleMessageActivityTemplate", typeof(DataTemplate), typeof(ModActivityTemplateSelector), new PropertyMetadata(null));

        public DataTemplate MessageHeaderActivityTemplate
        {
            get { return (DataTemplate)GetValue(MessageHeaderActivityProperty); }
            set { SetValue(MessageHeaderActivityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MessageHeaderActivity.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageHeaderActivityProperty =
            DependencyProperty.Register("MessageHeaderActivity", typeof(DataTemplate), typeof(ModActivityTemplateSelector), new PropertyMetadata(null));

     
        public DataTemplate MessagePartActivity
        {
            get { return (DataTemplate)GetValue(MessagePartActivityProperty); }
            set { SetValue(MessagePartActivityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MessagePartActivity.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessagePartActivityProperty =
            DependencyProperty.Register("MessagePartActivity", typeof(DataTemplate), typeof(ModActivityTemplateSelector), new PropertyMetadata(null));



        public DataTemplateSelector Selector { get; set; }

        public ModActivityTemplateSelector()
        {
            Selector = new ModActivityTemplateSelectorImpl(this);
        }
    }
    class ModActivityTemplateSelectorImpl : DataTemplateSelector
    {
        private ModActivityTemplateSelector _selector;
        public ModActivityTemplateSelectorImpl(ModActivityTemplateSelector selector)
        {
            _selector = selector;
        }
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            //decide if its a group or a single
            var group = item as ActivityGroupViewModel;
            if (group != null)
            {
                if (group.FirstActivity is PostedCommentActivityViewModel || group.FirstActivity is PostedLinkActivityViewModel)
                    return _selector.QueueActivityTemplate;
                else if (group.Activities.Count == 1)
                    return _selector.SingleMessageActivityTemplate;
                else
                    return _selector.MessageHeaderActivityTemplate;
            }
            var activity = item as ActivityViewModel;
            if (activity != null)
            {
                if (activity is ReportActivityViewModel)
                {
                    return _selector.ReportActivityTemplate;
                }
                else if (activity is MessageActivityViewModel)
                {
                    return _selector.MessagePartActivity;
                }
                else
                {
                    return _selector.ReportHeaderActivityTemplate;
                }
                
            }
            else
                throw new ArgumentOutOfRangeException();
        }
    }
}
