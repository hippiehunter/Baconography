using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace SnooStream.Converters
{
    public class BoolToAccentBrushConverter : IValueConverter
    {
        private static Brush PhoneAccentBrush;
        private static Brush PhoneForegroundBrush;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (PhoneAccentBrush == null || PhoneForegroundBrush == null)
            {
                PhoneAccentBrush = Application.Current.Resources["SystemColorControlAccentColor"] as Brush;
                PhoneForegroundBrush = Application.Current.Resources["PhoneForegroundBrush"] as Brush;
            }

            var boolValue = (value as Nullable<bool>) ?? false;
            if (boolValue)
            {
                return PhoneAccentBrush;
            }
            else
            {
                return PhoneForegroundBrush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
