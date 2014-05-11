using GalaSoft.MvvmLight;
using SnooStream.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class ImageViewModel : ContentViewModel
    {

        public ImageViewModel(ViewModelBase context, string url, string title, ImageSource imageSource, byte[] imageBytes) : base(context)
        {
            Url = url;
            ImageSource = imageSource;
            Title = title;
            Domain = new Uri(url).DnsSafeHost;
            Loaded = imageSource != null;

			if(imageBytes != null && imageBytes.Length > 6) //minimum to identify the image type
			{
				IsGif = CheckGif(imageBytes);
			}
        }

        public string Url { get; set; }
        public string Domain { get; set; }
        public string Title { get; set; }
        public bool IsGif { get; set; }
        public ImageSource ImageSource { get; set; }
        public PreviewImageSource Preview { get; set; }
        private static bool CheckGif(byte[] data)
        {
            return
                data[0] == 0x47 && // G
                data[1] == 0x49 && // I
                data[2] == 0x46 && // F
                data[3] == 0x38 && // 8
                (data[4] == 0x39 || data[4] == 0x37) && // 9 or 7
                data[5] == 0x61;   // a
        }

       

        private async Task<byte[]> LoadImage()
        {
            byte[] bytes = null;
            await SnooStreamViewModel.NotificationService.ReportWithProgress("loading from " + Domain,
                async (report) =>
                {
                    bytes = await SnooStreamViewModel.SystemServices.DownloadWithProgress(Url, (progress) => report(PreviewLoadPercent = progress), SnooStreamViewModel.UIContextCancellationToken);
                    if (bytes != null && bytes.Length > 6) //minimum to identify the image type
                    {
                        IsGif = CheckGif(bytes);
                    }
                });
            return bytes;
        }

        internal override async Task LoadContent()
        {
            ImageSource = new ImageSource(Url, await LoadImage());
            Preview = new PreviewImageSource(ImageSource);
        }
    }
}
