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
using Windows.UI.Xaml.Data;

namespace SnooStream.Converters
{
    public class LinkGlyphConverter : IValueConverter
    {
        

        public object Convert(object value, Type targetType, object parameter, string language)
        {
			return LinkGlyphUtility.GetLinkGlyph(value);
        }



        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
