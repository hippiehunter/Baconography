using SnooSharp;
using SnooStream.Common;
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
    public class AuthorFlairKindConverter : IValueConverter
    {
        static SolidColorBrush bg_none = new SolidColorBrush(Colors.Transparent);
        static SolidColorBrush bg_mod = new SolidColorBrush(Colors.Transparent);
        static SolidColorBrush bg_op;

        static AuthorFlairKindConverter()
        {
            if (Application.Current.Resources.ContainsKey("SystemColorControlAccentColor"))
                bg_op = Application.Current.Resources["SystemColorControlAccentColor"] as SolidColorBrush;
            else
                bg_op = Utility.GetColorFromHexa("#FFDAA520");
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var kind = (AuthorFlairKind)value;
            switch (kind)
            {
                case AuthorFlairKind.OriginalPoster:
                    return bg_op;
                case AuthorFlairKind.Moderator:
                    return bg_mod;
                case AuthorFlairKind.None:
                    return bg_none;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class ForegroundAuthorFlairKindConverter : IValueConverter
    {
        static SolidColorBrush fg_none;
        static SolidColorBrush fg_mod;
        static SolidColorBrush fg_op;

        static ForegroundAuthorFlairKindConverter()
        {
            
        }

        public void PopulateBrushes()
        {
            if (Application.Current.Resources.ContainsKey("SystemColorControlAccentColor"))
                fg_none = Application.Current.Resources["SystemColorControlAccentColor"] as SolidColorBrush;
            else
                fg_none = Utility.GetColorFromHexa("#FFDAA520");

            if (Application.Current.Resources.ContainsKey("PhoneForegroundBrush"))
                fg_op = Application.Current.Resources["PhoneForegroundBrush"] as SolidColorBrush;
            else
                fg_op = Utility.GetColorFromHexa("#FFDAA520");

            fg_mod = new SolidColorBrush(Colors.Green);
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var kind = (AuthorFlairKind)value;

            if (fg_none == null)
            {
                PopulateBrushes();
            }

            switch (kind)
            {
                case AuthorFlairKind.OriginalPoster:
                    return fg_op;
                case AuthorFlairKind.Moderator:
                    return fg_mod;
                case AuthorFlairKind.None:
                    return fg_none;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
