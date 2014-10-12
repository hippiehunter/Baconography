using CommonResourceAcquisition.VideoAcquisition;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel.Content
{
	public class VideoViewModel : ContentViewModel
	{
		private string _url;
		Lazy<IVideoResult> _videoResult;
		public string RedditThumbnail { get; set; }
		public VideoViewModel(string url, string redditThumbnail)
		{
			_url = url;
			_videoResult = new Lazy<IVideoResult>(() => CommonResourceAcquisition.VideoAcquisition.VideoAcquisition.GetVideo(_url));
		}

		private string _bestPlayableUrl;
		public string BestPlayableUrl
		{
			get
			{
				return _bestPlayableUrl;
			}
			set
			{
				_bestPlayableUrl = value;
				RaisePropertyChanged("BestPlayableUrl");
			}
		}

		internal Task<string> StillUrl()
		{
			return _videoResult.Value.PreviewUrl(CancelToken.Token);
		}

		protected override async Task StartLoad()
		{
			var playableStreams = await _videoResult.Value.PlayableStreams(CancelToken.Token);
			BestPlayableUrl = playableStreams.FirstOrDefault().Item1;
		}
	}
}
