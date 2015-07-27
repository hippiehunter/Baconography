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
    public class UserActivityTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SingleActivityTemplate { get; set; }
        public DataTemplate HeaderActivityTemplate { get; set; }
        public DataTemplate BodyActivityTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            //decide if its a group or a single
            var group = item as ActivityGroupViewModel;
            if (group != null)
            {
                if (group.Activities.Count == 1)
                    return SingleActivityTemplate;
                else
                    return HeaderActivityTemplate;
            }
            var activity = item as ActivityViewModel;
            if (activity != null)
            {
                return BodyActivityTemplate;
            }
            else
                throw new ArgumentOutOfRangeException();
        }
    }
}
