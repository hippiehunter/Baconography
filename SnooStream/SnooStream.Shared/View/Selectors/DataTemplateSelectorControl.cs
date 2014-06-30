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

		public bool IsLoaded
		{
			get { return (bool)GetValue(IsLoadedProperty); }
			set { SetValue(IsLoadedProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsLoaded.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsLoadedProperty =
			DependencyProperty.Register("IsLoaded", typeof(bool), typeof(DataTemplateSelectorControl), new PropertyMetadata(false, OnLoadedChanged));

		private static void OnLoadedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var thisp = d as DataTemplateSelectorControl;
			if (thisp != null)
			{
				thisp.ContentTemplate = thisp.SelectTemplate(thisp.Content, thisp);
			}
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
			if (IsLoaded)
				return SelectTemplateCore(item, container);
			
			else
				return null;
		}

        protected virtual DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return null;
        }
    }
}
