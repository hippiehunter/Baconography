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
    public class SubredditRiverTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate
        {
            get { return (DataTemplate)GetValue(NormalTemplateProperty); }
            set { SetValue(NormalTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NormalTemplateProperty =
            DependencyProperty.Register("TextTemplate", typeof(DataTemplate), typeof(SubredditRiverTemplateSelector), new PropertyMetadata(null));

        public DataTemplate ImagesTemplate
        {
            get { return (DataTemplate)GetValue(ImagesTemplateProperty); }
            set { SetValue(ImagesTemplateProperty, value); }
        }


        // Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImagesTemplateProperty =
            DependencyProperty.Register("ImagesTemplate", typeof(DataTemplate), typeof(SubredditRiverTemplateSelector), new PropertyMetadata(null));

        public DataTemplate MixedTemplate
        {
            get { return (DataTemplate)GetValue(MixedTemplateProperty); }
            set { SetValue(MixedTemplateProperty, value); }
        }


        // Using a DependencyProperty as the backing store for SelfContentTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MixedTemplateProperty =
            DependencyProperty.Register("MixedTemplate", typeof(DataTemplate), typeof(SubredditRiverTemplateSelector), new PropertyMetadata(null));

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var linkView = item as LinkViewModel;

            if (linkView == null)
                throw new ArgumentOutOfRangeException();

            var targetItem = linkView.Content;

            //if its a string its not full content
            if ((targetItem.PreviewImage != null && targetItem.PreviewImage is string) && targetItem.PreviewText != null)
                return MixedTemplate;
            else if (targetItem.PreviewImage != null && !(targetItem.PreviewImage is string))
                return ImagesTemplate;
            else
                return TextTemplate;

        }

    }
}
