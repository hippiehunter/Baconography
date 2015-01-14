using SnooDom;
using SnooStream.Common;
using SnooStream.View.Controls;
using SnooStream.View.Controls.CardView;
using SnooStream.ViewModel;
using SnooStream.ViewModel.Content;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace SnooStream.Converters
{
    public class ContentPreviewConverter : IValueConverter
    {
		public object Convert(object value, Type targetType, object parameter, string language)
		{
            return MakePreviewControl(value as LinkViewModel);
		}

		public static FrameworkElement MakePreviewControl(LinkViewModel linkViewModel, bool full = false)
		{
			var preview = Preview.LoadLinkPreview(linkViewModel.Content);
            if (full)
                preview.FinishLoad(SnooStreamViewModel.UIContextCancellationToken);
            if (linkViewModel.Content is SelfViewModel && full)
            {
                return new CardMarkdownControl { DataContext = ((SelfViewModel)linkViewModel.Content).Markdown };
            }
			if (preview is PreviewText)
			{
				return new CardPreviewTextControl { DataContext = preview };
			}
			else if (preview is PreviewImage)
			{
				return new CardPreviewImageControl { DataContext = preview };
			}
			else
			{
				throw new NotImplementedException(string.Format("cant convert value of type {0} to type of Preview", preview.GetType().FullName));
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
