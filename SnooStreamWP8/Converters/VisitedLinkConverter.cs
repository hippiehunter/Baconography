﻿using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SnooStreamWP8.Converters
{
    public class VisitedLinkConverter : IValueConverter
    {
        static SolidColorBrush history = new SolidColorBrush(Colors.Yellow);
        static SolidColorBrush noHistory = new SolidColorBrush(Colors.Orange);

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (SnooStreamViewModel.OfflineService.HasHistory(parameter as string))
                return history;
            else
                return noHistory;
                
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class VisitedMainLinkConverter : IValueConverter
    {
        static SolidColorBrush history = new SolidColorBrush(Colors.Gray);
        static Brush noHistory;

        public VisitedMainLinkConverter()
        {
            noHistory = Application.Current.Resources["PhoneForegroundBrush"] as Brush;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
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
            else
                return noHistory;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
