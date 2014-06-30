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
    public class CommentTemplateSelector : DataTemplateSelector
    {
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


        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is CommentViewModel)
                return CommentTemplate;
            else if (item is MoreViewModel)
                return MoreTemplate;
            else if (item is LoadFullCommentsViewModel)
                return LoadFullyTemplate;
            else throw new NotImplementedException();
        }
    }
}
