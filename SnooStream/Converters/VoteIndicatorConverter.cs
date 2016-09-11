using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace SnooStream.Converters
{
    public class VoteIndicatorConverter : IValueConverter
    {
        private static Brush OrangeRed = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x45, 0x00));
        private static Brush LightSkyBlue = new SolidColorBrush(Color.FromArgb(0xFF, 0x87, 0xCE, 0xFA));
        private static FontFamily SegoeUISymbol = new FontFamily("Segoe UI Symbol");
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var votable = value as VotableViewModel;
            if (votable != null && votable.LikeStatus != 0)
            {
                if (votable.LikeStatus == 1)
                {
                    return new TextBlock
                    {
                        Foreground = OrangeRed,
                        FontSize = 13,
                        Margin = new Thickness(0),
                        FontFamily = SegoeUISymbol,
                        Text = "\uE110"
                    };
                }
                else
                {
                    var newTextBlock = new TextBlock
                    {
                        Foreground = LightSkyBlue,
                        FontSize = 13,
                        Margin = new Thickness(0),
                        FontFamily = SegoeUISymbol,
                        Text = "\uE110"
                    };

                    newTextBlock.RenderTransform = new RotateTransform { Angle = 180, CenterX = 9, CenterY = 9 };
                    return newTextBlock;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class VoteUriConverter : IValueConverter
    {
        private static BitmapIcon UpFill = new BitmapIcon { UriSource = new Uri("ms-appx:///Assets/Buttons/updown_up_fill.png") };
        private static BitmapIcon Hollow = new BitmapIcon { UriSource = new Uri("ms-appx:///Assets/Buttons/updown_hollow.png") };
        private static BitmapIcon DownFill = new BitmapIcon { UriSource = new Uri("ms-appx:///Assets/Buttons/updown_down_fill.png") };
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var votable = value as VotableViewModel;
            //These are the wrong icons but they will work for now
            if (votable != null && votable.LikeStatus != 0)
            {
                if (votable.LikeStatus == 1)
                {
                    return UpFill;
                }
                else
                {
                    return DownFill;
                }
            }
            else
                return Hollow;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

	public class VoteSymbolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var votable = value as VotableViewModel;
			//These are the wrong icons but they will work for now
			if (votable != null && votable.LikeStatus != 0)
			{
				if (votable.LikeStatus == 1)
				{
					return new SymbolIcon(Symbol.Like);
				}
				else 
				{
					return new SymbolIcon(Symbol.Dislike);
				}
			}
			else
				return new SymbolIcon(Symbol.LikeDislike);
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
