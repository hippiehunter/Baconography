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
    public class VoteColorConverter : IValueConverter
    {
        private static Brush upvote = new SolidColorBrush(Colors.Orange);
        private static Brush downvote = new SolidColorBrush(Color.FromArgb(0xFF, 0x87, 0xCE, 0xFA));

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var votable = value as VotableViewModel;
            if (parameter == null)
            {
                if (votable != null && votable.LikeStatus != 0)
                {
                    if (votable.LikeStatus == 1)
                        return upvote;
                    if (votable.LikeStatus == -1)
                        return downvote;
                }
            }
            else
            {
                string voteParam = parameter as string;
                if (voteParam == "1")
                {
                    if (votable != null && votable.LikeStatus != 0)
                    {
                        if (votable.LikeStatus == 1)
                            return upvote;
                    }
                }
                else if (voteParam == "0")
                {
                    if (votable != null && votable.LikeStatus != 0)
                    {
                        if (votable.LikeStatus == -1)
                            return downvote;
                    }
                }
            }

            return (SolidColorBrush)Application.Current.Resources["PhoneForegroundBrush"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
