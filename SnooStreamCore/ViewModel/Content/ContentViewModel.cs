﻿using CommonResourceAcquisition.ImageAcquisition;
using CommonResourceAcquisition.VideoAcquisition;
using GalaSoft.MvvmLight;
using SnooSharp;
using SnooStream.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.ViewModel.Content
{
	public abstract class ContentViewModel : ViewModelBase
	{
		protected CancellationTokenSource CancelTokenSource = new CancellationTokenSource(SnooStreamViewModel.Settings.ContentTimeout);
		public string Glyph { get; set; }
		public string Title { get; set; }
        protected bool UIThreadLoad { get; set; }
        public string Url { get; set; }
        public CancellationToken CancelToken
        {
            get
            {
                return CancelTokenSource.Token;
            }
        }
        VotableViewModel Votable { get; set; }

		public static ContentViewModel MakeContentViewModel(string url, string title = null, ILinkViewModel selfLink = null, string redditThumbnail = null)
		{
			ContentViewModel result = null;
			string targetHost = null;
			string fileName = null;

			if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
			{
				var uri = new Uri(url);
				targetHost = uri.DnsSafeHost.ToLower();
				fileName = uri.AbsolutePath;
			}

			var glyph = LinkGlyphUtility.GetLinkGlyph((selfLink as object) ?? (url as object));

			if (selfLink is LinkViewModel && ((LinkViewModel)selfLink).IsSelfPost)
			{
				result = new SelfViewModel(((LinkViewModel)selfLink));
			}
			else if (LinkGlyphUtility.IsComment(url) ||
				LinkGlyphUtility.IsCommentsPage(url) ||
				LinkGlyphUtility.IsSubreddit(url) ||
				LinkGlyphUtility.IsUser(url) ||
				LinkGlyphUtility.IsUserMultiReddit(url))
			{
				result = new InternalRedditViewModel(url);
			}
			else if (fileName != null && 
				(fileName.EndsWith(".mp4") ||
				fileName.EndsWith(".gifv")))
			{
				result = new VideoViewModel(url, redditThumbnail);
			}
			else if (targetHost == "www.youtube.com" ||
				targetHost == "www.youtu.be" ||
				targetHost == "youtu.be" ||
				targetHost == "youtube.com" ||
				targetHost == "m.youtube.com" ||
				targetHost == "vimeo.com" ||
				targetHost == "www.vimeo.com" ||
				targetHost == "liveleak.com" ||
				targetHost == "www.liveleak.com" ||
				targetHost == "zippy.gfycat.com" ||
				targetHost == "fat.gfycat.com" ||
				targetHost == "giant.gfycat.com" ||
				targetHost == "www.gfycat.com" ||
				targetHost == "gfycat.com")
			{
				if (VideoAcquisition.IsAPI(url))
					result = new VideoViewModel(url, redditThumbnail);
				else
					result = new PlainWebViewModel(true, url, title, redditThumbnail);

			}
			else
			{
				if (ImageAcquisition.IsImageAPI(url))
				{
					result = new AlbumViewModel(url, title, redditThumbnail);
				}
				else if (fileName != null && 
					(fileName.EndsWith(".jpg") ||
					fileName.EndsWith(".png") ||
					fileName.EndsWith(".gif") ||
					fileName.EndsWith(".jpeg")))
				{
					result = new ImageViewModel(url, title, redditThumbnail);
				}
				else
					result = new PlainWebViewModel(true, url, title, redditThumbnail);
			}

			result.Glyph = glyph;
			if(selfLink != null)
			{
				result.Votable = selfLink.Votable;
				result.Title = selfLink.Title;
			}
            result.Url = url;
			return result;
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
			CancelTokenSource.Cancel();
		}

		protected void SetErrorStatus(string errorText)
		{

		}

		public void Retry()
		{
			CancelTokenSource = new CancellationTokenSource(SnooStreamViewModel.Settings.ContentTimeout);
		}

		public async void StartLoad(int? timeout)
		{
            if(CancelTokenSource.IsCancellationRequested)
			    CancelTokenSource = new CancellationTokenSource(timeout ?? SnooStreamViewModel.Settings.ContentTimeout);

			try
			{
                if (UIThreadLoad)
                {
                    await StartLoad();
                }
                else
                {
                    await Task.Run((Func<Task>)StartLoad);
                }
				
			}
			catch (Exception ex)
			{
				//_logger.Error("failed getting content", ex);
				SetErrorStatus(ex.Message);
			}
		}

		protected abstract Task StartLoad();
        private bool _focused;
        public virtual bool Focused
        {
            get
            {
                return _focused;
            }
            set
            {
                if(value)
                    SnooStreamViewModel.OfflineService.AddHistory(Url);
                _focused = value;
            }
        }

		public virtual void RefreshUnderlying()
		{
			RaisePropertyChanged("Title");
		}
	}
}
