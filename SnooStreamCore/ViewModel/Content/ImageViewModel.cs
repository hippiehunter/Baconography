using GalaSoft.MvvmLight;
using SnooStream.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel.Content
{
	public class ImageViewModel : ContentViewModel
	{
		public string Url;
		public string Title;
		public string RedditThumbnail;

		public ImageViewModel(string url, string title, string linkThumbnailUrl)
		{
			Url = url;
			Title = title;
			RedditThumbnail = linkThumbnailUrl;
		}

		public IImageLoader ImageLoader
		{
			get
			{
				return SnooStreamViewModel.SystemServices.DownloadImageWithProgress(Url, (progress) => { }, CancelToken.Token, (ex) =>
				{
					SetErrorStatus(ex.Message);
				});
			}
		}

		protected override Task StartLoad()
		{
			return Task.FromResult(true);//ImageLoader.ForceLoad(SnooStreamViewModel.Settings.ContentTimeout);
		}
	}
}
