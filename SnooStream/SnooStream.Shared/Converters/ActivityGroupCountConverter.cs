using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace SnooStream.Converters
{
    public class ActivityGroupCountConverter : IValueConverter
    {
        private string MessageGroupText(ActivityViewModel viewModel)
        {
            if (viewModel is MessageActivityViewModel || viewModel is ModeratorMessageActivityViewModel)
            {
                return "messages";
            }
            else if (viewModel is ModeratorActivityViewModel)
            {
                return "activities";
            }
            else if (viewModel is PostedLinkActivityViewModel || viewModel is PostedCommentActivityViewModel ||
                viewModel is RecivedCommentReplyActivityViewModel || viewModel is MentionActivityViewModel)
            {
                return "replies";
            }
            else
                throw new ArgumentOutOfRangeException();
        }

        private string NewnessGroupText(ActivityViewModel viewModel)
        {
            if (viewModel is MessageActivityViewModel || viewModel is ModeratorMessageActivityViewModel)
            {
                return "unread";
            }
            else if (viewModel is ModeratorActivityViewModel)
            {
                return "unreviewed";
            }
            else if (viewModel is PostedLinkActivityViewModel || viewModel is PostedCommentActivityViewModel ||
                viewModel is RecivedCommentReplyActivityViewModel || viewModel is MentionActivityViewModel)
            {
                return "unviewed";
            }
            else
                throw new ArgumentOutOfRangeException();
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int readCount = 0;

            var group = value as ActivityGroupViewModel;

            foreach (var activity in group.Activities)
            {
                readCount = activity.IsNew ? readCount + 1 : readCount;
            }

            var adjustedCount = group.FirstActivity is PostedLinkActivityViewModel ? group.Activities.Count - 1 : group.Activities.Count;

            return string.Format("{0} {1}, {2} {3}", adjustedCount, MessageGroupText(group.FirstActivity), adjustedCount - readCount, NewnessGroupText(group.FirstActivity));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
