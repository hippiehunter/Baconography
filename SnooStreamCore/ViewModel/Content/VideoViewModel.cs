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

        public bool IsLooping
        {
            get
            {
                return CommonResourceAcquisition.VideoAcquisition.VideoAcquisition.IsGifType(_url);
            }
        }

		internal Task<string> StillUrl()
		{
			return _videoResult.Value.PreviewUrl(CancelTokenSource.Token);
		}

		protected override async Task StartLoad()
		{
			var playableStreams = await _videoResult.Value.PlayableStreams(CancelTokenSource.Token);
			SnooStreamViewModel.SystemServices.RunUIAsync(() =>
				{
					if (playableStreams != null && playableStreams.Count() > 0)
					{
						BestPlayableUrl = playableStreams.FirstOrDefault().Item1;
					}
					return Task.FromResult(true);
				});
			
		}
	}
}
