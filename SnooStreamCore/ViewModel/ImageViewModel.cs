using GalaSoft.MvvmLight;
using SnooStream.Common;
using SnooStream.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class ImageViewModel : ContentViewModel
    {
        public ImageViewModel(ViewModelBase context, string url, string title, IImageLoader imageLoader) : base(context)
        {
            Url = url;
			ImageSource = imageLoader;
            Title = title;
            Domain = new Uri(url).DnsSafeHost;
        }

        public string Url { get; set; }
        public string Domain { get; set; }
        public string Title { get; set; }
		public IImageLoader ImageSource { get; set; }

		internal override Task LoadContent(bool previewOnly, Action<int> progress, CancellationToken cancelToken)
        {
			return Task.FromResult<bool>(true);
        }
    }
}
