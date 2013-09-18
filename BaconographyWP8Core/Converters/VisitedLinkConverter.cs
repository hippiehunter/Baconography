﻿using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using BaconographyWP8Core.Common;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using Windows.UI;

namespace BaconographyWP8.Converters
{
    public class VisitedLinkConverter : IValueConverter
    {
        static SolidColorBrush history = new SolidColorBrush(Colors.Yellow);
        static SolidColorBrush noHistory = new SolidColorBrush(Colors.Orange);

        IOfflineService _offlineService;
        public VisitedLinkConverter(IBaconProvider baconProvider)
        {
            _offlineService = baconProvider.GetService<IOfflineService>();
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (_offlineService.HasHistory(parameter as string))
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

        IOfflineService _offlineService;
        public VisitedMainLinkConverter()
        {
            noHistory = Styles.Resources["PhoneForegroundBrush"] as Brush;
			_offlineService = ServiceLocator.Current.GetInstance<IOfflineService>();
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                if (_offlineService.HasHistory(value as string))
                    return history;
                else
                    return noHistory;
            }
            else if(value is LinkViewModel)
            {
                var vm = value as LinkViewModel;
                if (_offlineService.HasHistory(vm.IsSelfPost ? vm.LinkThing.Data.Permalink : vm.Url))
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
