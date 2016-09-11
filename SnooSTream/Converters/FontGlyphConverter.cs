using SnooSharp;
using SnooStream.Common;
using SnooStream.Model;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace SnooStream.Converters
{
    public class FontGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is string ? new FontIcon { FontFamily = new FontFamily("Segoe UI Symbol"), Glyph = value as string } : value as IconElement;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
