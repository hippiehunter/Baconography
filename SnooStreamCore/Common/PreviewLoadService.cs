using GalaSoft.MvvmLight;
using SnooStream.ViewModel;
using SnooStream.ViewModel.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Common
{
	public class Preview : ViewModelBase
	{
		private string _thumbnailUrl;
		public string ThumbnailUrl
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
		public string Glyph { get; set; }

		public static Preview LoadLinkPreview(ContentViewModel content)
		{
			Preview result = null;
			if (content is ImageViewModel)
				LoadPreview(content as ImageViewModel, (PreviewImage)(result = new PreviewImage()));
			else if (content is AlbumViewModel)
				LoadPreview(content as AlbumViewModel, (PreviewImage)(result = new PreviewImage()));
			else if (content is PlainWebViewModel)
				LoadPreview(content as PlainWebViewModel, (PreviewText)(result = new PreviewText()));
			else if (content is InternalRedditViewModel)
				LoadPreview(content as InternalRedditViewModel, (PreviewText)(result = new PreviewText()));
			else if (content is VideoViewModel)
				LoadPreview(content as VideoViewModel, (PreviewImage)(result = new PreviewImage()));
			else if (content is SelfViewModel)
				LoadPreview(content as SelfViewModel, (PreviewText)(result = new PreviewText()));
			else
				throw new NotImplementedException("invalid content type");

			result.Glyph = content.Glyph;

			return result;
		}
		private static void LoadPreview(ImageViewModel imageViewModel, PreviewImage target)
		{
			target.ThumbnailUrl = imageViewModel.Url;
		}

		private static async void LoadPreview(AlbumViewModel albumViewModel, PreviewImage target)
		{
			try
			{
				target.ThumbnailUrl = await albumViewModel.FirstUrl();
			}
			catch(TaskCanceledException)
			{

			}
		}

		private static async void LoadPreview(VideoViewModel videoViewModel, PreviewImage target)
		{
			try
			{
				target.ThumbnailUrl = await videoViewModel.StillUrl();
			}
			catch (TaskCanceledException)
			{

			}
		}

		private static async void LoadPreview(PlainWebViewModel plainWebViewModel, PreviewText target)
		{
			try
			{
				target.Synopsis = await plainWebViewModel.FirstParagraph();
                target.ThumbnailUrl = await plainWebViewModel.FirstImage() ?? "ms-appx:///Assets/WebGlyphTile.png";
			}
			catch(TaskCanceledException)
			{

			}
		}

		private static void LoadPreview(SelfViewModel selfViewModel, PreviewText target)
		{
			target.Synopsis = selfViewModel.SelfText;
		}

		private static void LoadPreview(InternalRedditViewModel internalRedditViewModel, PreviewText target)
		{
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
				RaisePropertyChanged("Synopsis");
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
