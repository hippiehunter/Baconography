using CommonResourceAcquisition.ImageAcquisition;
using CommonResourceAcquisition.VideoAcquisition;
using GalaSoft.MvvmLight;
using SnooStream.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.ViewModel.Content
{
	public class ContentViewModel : ViewModelBase
	{
		protected CancellationTokenSource CancelToken = new CancellationTokenSource(SnooStreamViewModel.Settings.ContentTimeout);
		public static ContentViewModel MakeContentViewModel(string url, string title = null, LinkViewModel selfLink = null, string redditThumbnail = null)
		{
			string targetHost = null;
			string fileName = null;

			if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
			{
				var uri = new Uri(url);
				targetHost = uri.DnsSafeHost.ToLower();
				fileName = uri.AbsolutePath;
			}


			if (selfLink != null)
			{
				return new SelfViewModel(selfLink);
			}
			else if (LinkGlyphUtility.IsComment(url) ||
				LinkGlyphUtility.IsCommentsPage(url) ||
				LinkGlyphUtility.IsSubreddit(url) ||
				LinkGlyphUtility.IsUser(url) ||
				LinkGlyphUtility.IsUserMultiReddit(url))
			{
				return new InternalRedditViewModel(url);
			}
			else if (targetHost == "www.youtube.com" ||
				targetHost == "www.youtu.be" ||
				targetHost == "youtu.be" ||
				targetHost == "youtube.com" ||
				targetHost == "vimeo.com" ||
				targetHost == "www.vimeo.com" ||
				targetHost == "liveleak.com" ||
				targetHost == "www.liveleak.com")
			{
				if (VideoAcquisition.IsAPI(url))
					return new VideoViewModel(url);
				else
					return new PlainWebViewModel(true, url, redditThumbnail);

			}
			else
			{
				if (ImageAcquisition.IsImageAPI(url))
				{
					return new AlbumViewModel(url, title, redditThumbnail);
				}
				else if (fileName.EndsWith(".jpg") ||
					fileName.EndsWith(".png") ||
					fileName.EndsWith(".gif") ||
					fileName.EndsWith(".jpeg"))
				{
					return new ImageViewModel(url, title, redditThumbnail);
				}
				else
					return new PlainWebViewModel(true, url, redditThumbnail);
			}

		}

		private int _progress = 0;
		public int Progress
		{
			get
			{
				return _progress;
			}
			set
			{
				_progress = value;
				RaisePropertyChanged("Progress");
			}
		}

		public void CancelLoad()
		{
			CancelToken.Cancel();
		}

		protected void SetErrorStatus(string errorText)
		{

		}

		public void Retry()
		{
			CancelToken = new CancellationTokenSource(SnooStreamViewModel.Settings.ContentTimeout);
		}
	}
}
