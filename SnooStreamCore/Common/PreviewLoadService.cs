using GalaSoft.MvvmLight;
using SnooStream.ViewModel;
using SnooStream.ViewModel.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.Common
{
	public class Preview : ViewModelBase
	{
		private object _thumbnailUrl;
		public object ThumbnailUrl
		{
			get
			{
				return _thumbnailUrl;
			}
			set
			{
				_thumbnailUrl = value;
				RaisePropertyChanged("ThumbnailUrl");
			}
		}
        private object _hqthumbnailUrl;
        public object HQThumbnailUrl
        {
            get
            {
                return _hqthumbnailUrl;
            }
            set
            {
                _hqthumbnailUrl = value;
                RaisePropertyChanged("HQThumbnailUrl");
            }
        }

		public string Glyph { get; set; }

		public Func<CancellationToken, Task<string>> FinishLoad { get; private set; }
		public static Preview LoadLinkPreview(ContentViewModel content)
		{
			Preview result = null;
			if (content is ImageViewModel)
			{
				result = new PreviewImage { ThumbnailUrl = ((ImageViewModel)content).RedditThumbnail };
				result.FinishLoad = (cancel) => LoadPreview(content as ImageViewModel, result as PreviewImage); 
			}
			else if (content is AlbumViewModel)
			{
				result = new PreviewImage { ThumbnailUrl = ((AlbumViewModel)content).RedditThumbnail };
				result.FinishLoad = (cancel) => LoadPreview(content as AlbumViewModel, result as PreviewImage, cancel); 
			}
			else if (content is PlainWebViewModel)
			{
                result = new PreviewText { ThumbnailUrl = String.IsNullOrEmpty(((PlainWebViewModel)content).RedditThumbnail) ? "ms-appx:///Assets/WebGlyphTile.png" : ((PlainWebViewModel)content).RedditThumbnail };
				result.FinishLoad = (cancel) => LoadPreview(content as PlainWebViewModel, result as PreviewText, cancel);
			}
			else if (content is InternalRedditViewModel)
			{
				result = new PreviewText { };
				result.FinishLoad = (cancel) => LoadPreview(content as InternalRedditViewModel, result as PreviewText, cancel);
			}
			else if (content is VideoViewModel)
			{
				result = new PreviewImage { ThumbnailUrl = ((VideoViewModel)content).RedditThumbnail };
				result.FinishLoad = (cancel) => LoadPreview(content as VideoViewModel, result as PreviewImage, cancel); 
			}
			else if (content is SelfViewModel)
			{
                result = new PreviewText { };
				result.FinishLoad = (cancel) => LoadPreview(content as SelfViewModel, result as PreviewText, cancel);
			}
			else
				throw new NotImplementedException("invalid content type");

			result.Glyph = content.Glyph;

			return result;
		}
		private static Task<string> LoadPreview(ImageViewModel imageViewModel, PreviewImage target)
		{
			return Task.FromResult(imageViewModel.Url);
		}

		private static async Task<string> LoadPreview(AlbumViewModel albumViewModel, PreviewImage target, CancellationToken cancel)
		{
			try
			{
				return await albumViewModel.FirstUrl();
			}
			catch(TaskCanceledException)
			{
				return null;
			}
		}

		private static async Task<string> LoadPreview(VideoViewModel videoViewModel, PreviewImage target, CancellationToken cancel)
		{
			try
			{
				return await videoViewModel.StillUrl();
			}
			catch (TaskCanceledException)
			{
				return null;
			}
		}

		private static async Task<string> LoadPreview(PlainWebViewModel plainWebViewModel, PreviewText target, CancellationToken cancel)
		{
			try
			{
				target.Synopsis = await plainWebViewModel.FirstParagraph();
				if (String.IsNullOrEmpty(plainWebViewModel.RedditThumbnail))
					return await plainWebViewModel.FirstImage();
				else
					return null;
			}
			catch(TaskCanceledException)
			{
				return null;
			}
		}

		private static Task<string> LoadPreview(SelfViewModel selfViewModel, PreviewText target, CancellationToken cancel)
		{
			target.Synopsis = selfViewModel.SelfText;
			return Task.FromResult<string>(null);
		}

		private static Task<string> LoadPreview(InternalRedditViewModel internalRedditViewModel, PreviewText target, CancellationToken cancel)
		{
			return Task.FromResult<string>(null);
			//TODO ??
		}
	}
	public class PreviewText : Preview
	{
		private string _synopsis;
		public string Synopsis
		{
			get
			{
				return _synopsis;
			}
			set
			{
				_synopsis = value;
				SnooStreamViewModel.SystemServices.QueueNonCriticalUI(() => RaisePropertyChanged("Synopsis"));
			}
		}
	}

	public class PreviewImage : Preview
	{
		private string _playLogo;
		public string PlayLogo
		{
			get
			{
				return _playLogo;
			}
			set
			{
				_playLogo = value;
				RaisePropertyChanged("PlayLogo");
			}
		}
	}
}
