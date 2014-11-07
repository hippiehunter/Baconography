using SnooStream.View.Controls;
using SnooStream.View.Controls.Content;
using SnooStream.ViewModel;
using SnooStream.ViewModel.Content;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Data;

namespace SnooStream.Converters
{
    public class ContentConverter : IValueConverter
    {
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var linkViewModel = value as ILinkViewModel;
			var content = linkViewModel.Content;
			content.StartLoad(SnooStreamViewModel.Settings.ContentTimeout);
			if (content is ImageViewModel)
			{
				var container = new ImageContainer();
				container.containerSlideView.ItemsSource = new List<ContentViewModel> { content };
				return container;
			}
			else if (content is AlbumViewModel)
				return new AlbumControl { DataContext = content };
			else if (content is VideoViewModel)
				return new VideoControl { DataContext = content };
			else if (content is PlainWebViewModel)
                return new PlainWebControl { DataContext = content };
			else if (content is SelfViewModel)
				return new CommentsView { DataContext = content };
			else
				throw new NotImplementedException();
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
