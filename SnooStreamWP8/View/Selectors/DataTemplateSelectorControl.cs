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
	public class DataTemplateSelectorControl : ContentControl
    {
        public DataTemplateSelectorControl()
            : base()
        {

        }

		protected override void OnContentChanged (object oldContent, object newContent)
		{
			base.OnContentChanged(oldContent, newContent);
			if (newContent == null)
			{
				ContentTemplate = null;
			}
			else
			{
				ContentTemplate = SelectTemplate(newContent, this);
			}
		}

		public DataTemplate SelectTemplate (object item, DependencyObject container)
		{
			return SelectTemplateCore(item, container);
		}

        protected virtual DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return null;
        }
    }
}
