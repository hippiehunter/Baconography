using GalaSoft.MvvmLight;
using SnooSharp;
using SnooStream.Common;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace SnooStream.Converters
{
    public class VisitedLinkConverter : IValueConverter
    {
        public OfflineService Offline { get; set; }
        static SolidColorBrush history = new SolidColorBrush(Colors.Yellow);
        static SolidColorBrush noHistory = new SolidColorBrush(Colors.Orange);

        public object Convert(object value, Type targetType, object parameter, string language)
        {
			if (!ViewModelBase.IsInDesignModeStatic && Offline.HasHistory(parameter as string))
				return history;
			else
				return noHistory;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class VisitedMainLinkConverter : IValueConverter
    {
        public OfflineService Offline { get; set; }

        static SolidColorBrush darkVisited = new SolidColorBrush(Colors.Gray);
        static SolidColorBrush lightVisited = new SolidColorBrush(Colors.Gray);
        static Brush darkNew = new SolidColorBrush(Colors.White);
        static Brush lightNew = new SolidColorBrush(Colors.Black);

        public VisitedMainLinkConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var noHistory = Application.Current.RequestedTheme == ApplicationTheme.Dark ? darkNew : lightNew;
            var history = Application.Current.RequestedTheme == ApplicationTheme.Dark ? darkVisited : lightVisited;

			if(ViewModelBase.IsInDesignModeStatic)
			{
				return noHistory;
			}
            if (value is string)
            {
                if (Offline.HasHistory(value as string))
                    return history;
                else
                    return noHistory;
            }
            else if (value is LinkViewModel)
            {
                var vm = value as LinkViewModel;
                if (Offline.HasHistory(vm.Thing.IsSelf ? vm.Thing.Permalink : vm.Thing.Url) || (vm.Thing.Visited ?? false))
                    return history;
                else
                    return noHistory;
            }
            else if (value is ActivityViewModel && ((ActivityViewModel)value).Thing.Data is Link)
            {
                var link = ((ActivityViewModel)value).Thing.Data as Link;
                if (Offline.HasHistory(link.IsSelf ? link.Permalink : link.Url) || (link.Visited ?? false))
                    return history;
                else
                    return noHistory;
            }
            else
                return noHistory;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
