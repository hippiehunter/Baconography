using SnooSharp;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace SnooStream.Converters
{
    public class ActivityGroupCountConverter : IValueConverter
    {
        private string MessageGroupText(ActivityViewModel viewModel)
        {
            if (viewModel.Thing.Data is Message)
            {
                return "messages";
            }
            else if (viewModel.Thing.Data is Link)
            {
                return "replies";
            }
            else
                throw new ArgumentOutOfRangeException();
        }

        private string NewnessGroupText(ActivityViewModel viewModel)
        {
            if (viewModel.Thing.Data is Message)
            {
                return "unread";
            }
            else if (viewModel.Thing.Data is Link)
            {
                return "unviewed";
            }
            else
                throw new ArgumentOutOfRangeException();
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return value;

            var group = value as ActivityHeaderViewModel;
            return string.Format("{0} {1}, {2} {3}", group.UnreadCount + group.ReadCount, "messages", group.ReadCount, "unread");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
