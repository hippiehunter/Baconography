﻿using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public class CommentDepthColorConverter : IValueConverter
    {
		static SolidColorBrush transparent = new SolidColorBrush(Colors.Transparent);

        static List<SolidColorBrush> depthBrushes = new List<SolidColorBrush>();
        static SolidColorBrush accentBrush = null;
        private void PopulateBrushes()
        {
            var currentAccentColor = Application.Current.Resources["SystemColorControlAccentColor"] as Nullable<Windows.UI.Color> ?? Colors.AliceBlue;
            if (accentBrush != null && currentAccentColor == accentBrush.Color)
                return;

            depthBrushes.Clear();
            depthBrushes.Add(new SolidColorBrush(Colors.Transparent));
            for (double i = 0.2; i <= 2.0; i+= 0.2)
            {
                if (i < 1)
                {
                    int r = currentAccentColor.R;
                    r = (int)(r == 0 ? 10 * i : r * i);
                    r = r > 255 ? 255 : r;
                    int g = currentAccentColor.G;
                    g = (int)(g == 0 ? 10 * i : g * i);
                    g = g > 255 ? 255 : g;
                    int b = currentAccentColor.B;
                    b = (int)(b == 0 ? 10 * i : b * i);
                    b = b > 255 ? 255 : b;
                    depthBrushes.Add(new SolidColorBrush(Color.FromArgb((byte)255, (byte)r, (byte)g, (byte)b)));
                }
                else if (i == 1)
                {
                    depthBrushes.Add(new SolidColorBrush(currentAccentColor));
                }
                else
                {
                    double r = (255 - currentAccentColor.R);
                    r = r == 0 ? 10 : r;
                    r *= (i - 1);
                    r += currentAccentColor.R;
                    r = r > 255 ? 255 : r;
                    double g = (255 - currentAccentColor.G);
                    g = g == 0 ? 10 : g;
                    g *= (i - 1);
                    g += currentAccentColor.G;
                    g = g > 255 ? 255 : g;
                    double b = (255 - currentAccentColor.B);
                    b = b == 0 ? 10 : b;
                    b *= (i - 1);
                    b += currentAccentColor.B;
                    b = b > 255 ? 255 : b;
                    depthBrushes.Add(new SolidColorBrush(Color.FromArgb((byte)255, (byte)r, (byte)g, (byte)b)));
                }
            }

            accentBrush = new SolidColorBrush(currentAccentColor);
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
#if WINDOWS_APP
			bool multiColor = true;
#else
			bool multiColor = SnooStreamViewModel.Settings.MultiColorCommentMargins;
#endif

			if (multiColor)
            {
                int depth = (int)value;
                switch (depth)
                {
                    case 0:
                        return zero;
                    case 1:
                        return one;
                    case 2:
                        return two;
                    case 3:
                        return three;
                    case 4:
                        return four;
                    case 5:
                        return five;
                    case 6:
                        return six;
                    case 7:
                        return seven;
                    case 8:
                        return eight;
                    case 9:
                        return nine;
                    case 10:
                        return ten;

                    default:
                        return zero;
                }
            }
            else
            {
                PopulateBrushes();
                int depth = (int)value;
                switch (depth)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                    case 10:
                        return depthBrushes[depth];
                    default:
                        return depthBrushes[0];
                }
            }
        }


        static SolidColorBrush zero = new SolidColorBrush(Colors.Gray);
        static SolidColorBrush one = new SolidColorBrush(Color.FromArgb(255, 98, 170, 42));
        static SolidColorBrush two = new SolidColorBrush(Color.FromArgb(255, 172, 43, 80));
        static SolidColorBrush three = new SolidColorBrush(Color.FromArgb(255, 191, 84, 48));
        static SolidColorBrush four = new SolidColorBrush(Color.FromArgb(255, 64, 147, 00));
        static SolidColorBrush five = new SolidColorBrush(Color.FromArgb(255, 149, 00, 43));
        static SolidColorBrush six = new SolidColorBrush(Color.FromArgb(255, 166, 42, 00));
        static SolidColorBrush seven = new SolidColorBrush(Color.FromArgb(255, 0, 115, 60));
        static SolidColorBrush eight = new SolidColorBrush(Color.FromArgb(255, 33, 133, 85));
        static SolidColorBrush nine = new SolidColorBrush(Color.FromArgb(255, 255, 150, 64));
        static SolidColorBrush ten = new SolidColorBrush(Color.FromArgb(255, 255, 150, 64));

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

