using SnooStream.Common;
using SnooStream.View.Controls;
using SnooStream.ViewModel;
using SnooStream.ViewModel.Content;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Data;

namespace SnooStream.Converters
{
    public class ContentPreviewConverter : IValueConverter
    {
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var linkViewModel = value as LinkViewModel;
			var preview = Preview.LoadLinkPreview(linkViewModel.Content);
			if (preview is PreviewText)
			{
				return new PreviewTextControl { DataContext = preview };
			}
			else if (preview is PreviewImage)
			{
				return new PreviewImageControl { DataContext = preview };
			}
			else
			{
				throw new NotImplementedException(string.Format("cant convert value of type {0} to type of Preview", value.GetType().FullName));
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
