using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace SnooStream.Converters
{
    public class DomainConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var valueString = value as string;
            if (valueString != null && Uri.IsWellFormedUriString(valueString, UriKind.Absolute))
            {
                var uri = new Uri(valueString);
                return string.Format("({0})", uri.DnsSafeHost);
            }
            else
                return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
