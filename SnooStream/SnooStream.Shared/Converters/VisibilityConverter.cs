using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace SnooStream.Converters
{
    public class BooleanVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool original = (bool)value;
            return original ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class InvertedBooleanVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool original = (bool)value;
            return original ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

	public class BlankVisibilityConverter : IValueConverter
	{
        public object Convert(object value, Type targetType, object parameter, string language)
        {
			try
			{
				var valueStr = value as dynamic;
				return string.IsNullOrWhiteSpace((string)valueStr.Key) ? Visibility.Collapsed : Visibility.Visible;
			}
			catch
			{
				return Visibility.Collapsed;
			}
		}

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
