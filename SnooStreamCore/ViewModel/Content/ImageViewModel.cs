﻿using GalaSoft.MvvmLight;
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
		public string Title {get; set;}
		public string RedditThumbnail {get; set;}
        public string HQThumbnail { get; set; }

		public ImageViewModel(string url, string title, string linkThumbnailUrl)
		{
			Url = url;
			Title = title;
			RedditThumbnail = linkThumbnailUrl;
		}

		protected override Task StartLoad()
		{
			return Task.FromResult(true);//ImageLoader.ForceLoad(SnooStreamViewModel.Settings.ContentTimeout);
		}
	}
}
