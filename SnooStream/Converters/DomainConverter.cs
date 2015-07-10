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
			return GetDomain(value);
        }

		public static string GetDomain(object value)
		{
			var valueString = value as string;
			if (valueString != null && Uri.IsWellFormedUriString(valueString, UriKind.Absolute))
			{
				var uri = new Uri(valueString);
				return uri.DnsSafeHost;
			}
			else
				return "";
		}

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
