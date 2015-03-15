using GalaSoft.MvvmLight;
using SnooStream.ViewModel;
using SnooStream.ViewModel.Content;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

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
        private string _hqthumbnailUrl;
        public string HQThumbnailUrl
        {
            get
            {
                return _hqthumbnailUrl;
            }
            set
            {
                _hqthumbnailUrl = value;
                if (!string.IsNullOrWhiteSpace(value))
                    IsFullyLoaded = true;

                RaisePropertyChanged("HQThumbnailUrl");
            }
        }

        public bool IsFullyLoaded { get; set; }

		public string Glyph { get; set; }

		public Func<CancellationToken, Task> FinishLoad { get; private set; }
		public static Preview LoadLinkPreview(ContentViewModel content)
		{
			Preview result = null;
			if (content is ImageViewModel)
			{
                result = new PreviewImage { ThumbnailUrl = ((ImageViewModel)content).RedditThumbnail, HQThumbnailUrl = ((ImageViewModel)content).HQThumbnail };
				result.FinishLoad = (cancel) => LoadPreview(content as ImageViewModel, result as PreviewImage, cancel); 
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
		private static async Task LoadPreview(ImageViewModel imageViewModel, PreviewImage target, CancellationToken cancel)
		{
            if (!string.IsNullOrWhiteSpace(imageViewModel.HQThumbnail) || string.IsNullOrWhiteSpace(imageViewModel.Url) || cancel.IsCancellationRequested)
                return;

            try
            {
                var previewUrl = await SnooStreamViewModel.SystemServices.ImagePreviewFromUrl(imageViewModel.Url, cancel);
                if(!cancel.IsCancellationRequested)
                    SnooStreamViewModel.SystemServices.QueueNonCriticalUI(() => target.HQThumbnailUrl = previewUrl);
            }
            catch (OperationCanceledException)
            {
                //Do nothing
            }
		}

		private static async Task LoadPreview(AlbumViewModel albumViewModel, PreviewImage target, CancellationToken cancel)
		{
			try
			{
				var previewUrl = await albumViewModel.FirstUrl();
                if (!cancel.IsCancellationRequested)
                    SnooStreamViewModel.SystemServices.QueueNonCriticalUI(() => target.HQThumbnailUrl = previewUrl);
			}
			catch(TaskCanceledException)
			{
			}
		}

		private static async Task LoadPreview(VideoViewModel videoViewModel, PreviewImage target, CancellationToken cancel)
		{
			try
			{
                var previewUrl = await videoViewModel.StillUrl();
                if (!cancel.IsCancellationRequested)
                    SnooStreamViewModel.SystemServices.QueueNonCriticalUI(() => target.HQThumbnailUrl = previewUrl);
			}
			catch (TaskCanceledException)
			{
			}
		}

		private static async Task LoadPreview(PlainWebViewModel plainWebViewModel, PreviewText target, CancellationToken cancel)
		{
			try
			{
				target.Synopsis = await plainWebViewModel.FirstParagraph();
                if (String.IsNullOrEmpty(plainWebViewModel.RedditThumbnail))
                {
                    var previewUrl = await plainWebViewModel.FirstImage();
                    SnooStreamViewModel.SystemServices.QueueNonCriticalUI(() => target.ThumbnailUrl = previewUrl);
                    target.IsFullyLoaded = true;
                }
			}
			catch(TaskCanceledException)
			{
			}
		}

		private static Task LoadPreview(SelfViewModel selfViewModel, PreviewText target, CancellationToken cancel)
		{
			target.BindChangeHandler(selfViewModel, "SelfText");
			target.Synopsis = selfViewModel.SelfText;
            target.IsFullyLoaded = true;
            return Task.FromResult<string>(null);
		}

		private static Task LoadPreview(InternalRedditViewModel internalRedditViewModel, PreviewText target, CancellationToken cancel)
		{
			return Task.FromResult<string>(null);
			//TODO ??
		}
	}
	public class PreviewText : Preview
	{
		internal INotifyPropertyChanged ObjectSource;
		internal PropertyChangedEventHandler _changeHandler;
		internal string TargetProperty;
		private string _synopsis;

		public void BindChangeHandler(INotifyPropertyChanged objectSource, string targetProperty)
		{
			TargetProperty = targetProperty;
			ObjectSource = objectSource;
			_changeHandler = ChangeHandler;
			ObjectSource.PropertyChanged += _changeHandler;
		}

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

		private void ChangeHandler(object sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == TargetProperty)
			{
				Synopsis = ObjectSource.GetType().GetTypeInfo().GetDeclaredProperty(TargetProperty).GetValue(ObjectSource, null) as string;
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
