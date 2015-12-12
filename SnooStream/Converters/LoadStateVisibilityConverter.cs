using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace SnooStream.Converters
{
    public class LoadStateVisibilityConverter : IValueConverter
    {
        public LoadState TargetState { get; set; } = LoadState.Loaded;
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return ((LoadState)value) == TargetState ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class NotLoadStateVisibilityConverter : IValueConverter
    {
        public LoadState TargetState { get; set; } = LoadState.Loaded;
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return ((LoadState)value) == TargetState ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class LoadedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is SubredditRiverViewModel)
            {
                if (((SubredditRiverViewModel)value).LoadState.State == LoadState.Loaded)
                    return value;
                else
                    return ((SubredditRiverViewModel)value).LoadState;
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
