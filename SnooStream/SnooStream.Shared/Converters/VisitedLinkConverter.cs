using GalaSoft.MvvmLight;
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
        static SolidColorBrush history = new SolidColorBrush(Colors.Yellow);
        static SolidColorBrush noHistory = new SolidColorBrush(Colors.Orange);

        public object Convert(object value, Type targetType, object parameter, string language)
        {
			if (!ViewModelBase.IsInDesignModeStatic && SnooStreamViewModel.OfflineService.HasHistory(parameter as string))
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
                if (SnooStreamViewModel.OfflineService.HasHistory(value as string))
                    return history;
                else
                    return noHistory;
            }
            else if (value is LinkViewModel)
            {
                var vm = value as LinkViewModel;
                if (SnooStreamViewModel.OfflineService.HasHistory(vm.Link.IsSelf ? vm.Link.Permalink : vm.Link.Url) || (vm.Link.Visited ?? false))
                    return history;
                else
                    return noHistory;
            }
            else if (value is PostedLinkActivityViewModel)
            {
                var vm = value as PostedLinkActivityViewModel;
                if (SnooStreamViewModel.OfflineService.HasHistory(vm.Link.IsSelf ? vm.Link.Permalink : vm.Link.Url) || (vm.Link.Visited ?? false))
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
