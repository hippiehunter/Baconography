using SnooStream.Common;
using SnooStream.View.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Data;

namespace SnooStream.Converters
{
    public class ContentPreviewConverter : IValueConverter
    {
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is PreviewText)
			{
				return new PreviewTextControl { DataContext = value };
			}
			else if (value is PreviewImage)
			{
				return new PreviewImageControl { DataContext = value };
			}
			else
			{
				throw new NotImplementedException(string.Format("cant convert value of type {0} to type of Preview", value.GetType().FullName));
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
