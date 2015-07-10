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
        private static Brush darkUpvote = new SolidColorBrush(Colors.Orange); //FFFFA500
        private static Brush lightUpvote = new SolidColorBrush(Color.FromArgb(0xFF, 0xCC, 0x72, 0x00));
        private static Brush darkDownvote = new SolidColorBrush(Color.FromArgb(0xFF, 0x87, 0xCE, 0xFA));
        private static Brush lightDownvote = new SolidColorBrush(Color.FromArgb(0xFF, 0x54, 0x9B, 0xC7));
        private static Brush darkNeutral = new SolidColorBrush(Colors.White);
        private static Brush lightNeutral = new SolidColorBrush(Colors.Black);

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var votable = value as VotableViewModel;
            var downvote = Application.Current.RequestedTheme == ApplicationTheme.Dark ? darkDownvote : lightDownvote;
            var upvote = Application.Current.RequestedTheme == ApplicationTheme.Dark ? darkUpvote : lightUpvote;
            var neutral = Application.Current.RequestedTheme == ApplicationTheme.Dark ? darkNeutral : lightNeutral;

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

            return neutral;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
