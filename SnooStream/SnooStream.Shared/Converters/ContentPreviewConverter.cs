using SnooDom;
using SnooStream.Common;
using SnooStream.View.Controls;
using SnooStream.View.Controls.CardView;
using SnooStream.ViewModel;
using SnooStream.ViewModel.Content;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace SnooStream.Converters
{
    public class ContentPreviewConverter
    {
        private static async void FinishLoad(Preview preview, CancellationToken cancelToken)
        {
            try
            {
                await preview.FinishLoad(cancelToken);
            }
            catch { }
        }

		public static async Task<FrameworkElement> MakePreviewControl(LinkViewModel linkViewModel, CancellationToken cancelSource, object existingControl, bool full = false)
        {
            var preview = await SnooStreamViewModel.SystemServices.RunAsyncIdle(() => 
                {
                    var result = Preview.LoadLinkPreview(linkViewModel.Content);
                    FinishLoad(result, cancelSource);
                    return result;
                }, cancelSource);
            if (linkViewModel.Content is SelfViewModel && full)
            {
                if (existingControl is CardMarkdownControl)
                {
					((CardMarkdownControl)existingControl).SetBinding(FrameworkElement.DataContextProperty, new Binding() { Path = new PropertyPath("Markdown"), Source = linkViewModel.Content } );
                    return existingControl as FrameworkElement;
                }
                else
                {
                    var newControl = new CardMarkdownControl();
					((CardMarkdownControl)newControl).SetBinding(FrameworkElement.DataContextProperty, new Binding() { Path = new PropertyPath("Markdown"), Source = linkViewModel.Content });
					return newControl;
                }
            }
			if (preview is PreviewText)
			{
                if (existingControl is CardPreviewTextControl)
                {
                    ((CardPreviewTextControl)existingControl).DataContext = preview;
                    return existingControl as FrameworkElement;
                }
                else
                {
                    return new CardPreviewTextControl { DataContext = preview, MaxHeight = 175 };
                }
			}
			else if (preview is PreviewImage)
			{
                if (existingControl is CardPreviewImageControl)
                {

                    ((CardPreviewImageControl)existingControl).DataContext = preview;
                    return existingControl as FrameworkElement;
                }
                else
                {
                    return new CardPreviewImageControl { DataContext = preview };
                }
			}
			else
			{
				throw new NotImplementedException(string.Format("cant convert value of type {0} to type of Preview", preview.GetType().FullName));
			}
		}
	}
}
