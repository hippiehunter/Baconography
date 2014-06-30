using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnooStream.Common;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace SnooStream.Converters
{
    public class AsyncImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var result = new BitmapImage();
            var imageSource = value as SnooStream.Common.ImageSource;
            if (imageSource != null)
            {
                imageSource.ImageData.ContinueWith(tsk =>
                    {
                        var tskResult = tsk.TryValue();
                        if (tskResult != null)
                            result.SetSource(new MemoryStream(tskResult).AsRandomAccessStream());
                        else if (Uri.IsWellFormedUriString(imageSource.UrlSource, UriKind.Absolute))
                            result.UriSource = new Uri(imageSource.UrlSource);
                    }, SnooStreamViewModel.UIScheduler);
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
