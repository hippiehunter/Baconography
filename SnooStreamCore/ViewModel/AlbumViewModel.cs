﻿using GalaSoft.MvvmLight;
using SnooStream.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class AlbumViewModel : ContentViewModel
    {
        public AlbumViewModel(ViewModelBase context, string originalUrl, IEnumerable<Tuple<string, string>> apiResults, string albumTitle) : base(context)
        {
			LoadContextToken = Url = originalUrl;
            Domain = new Uri(originalUrl).Host;
            Title = albumTitle;
            Images = new ObservableCollection<ContentViewModel>();
            ApiResults = apiResults.Where(tpl => Uri.IsWellFormedUriString(tpl.Item2, UriKind.Absolute)).ToList();
            ApiImageCount = ApiResults.Count();
            if (ApiImageCount == 0)
                throw new Exception(string.Format("Invalid Album {0}", originalUrl));
        }

		private async Task LoadAlbumImpl(Action<int> progress, CancellationToken cancelToken)
        {
            int i = 0;
            foreach (var tpl in ApiResults)
            {
                if(Uri.IsWellFormedUriString(tpl.Item2, UriKind.Absolute))
                {
                    var imageUri = new Uri(tpl.Item2);
                    //make sure we havent already loaded this image
					if (Images.Count <= i)
					{
						if (await LoadImageImpl(tpl.Item1, imageUri, false, progress, cancelToken))
							i++;
					}
					else
						i++;
                }
                
            }
        }
		private async Task<bool> LoadImageImpl(string title, Uri source, bool isPreview, Action<int> progress, CancellationToken cancelToken)
        {
            bool loadedOne = false;
			var imageLoader = SnooStreamViewModel.SystemServices.DownloadImageWithProgress(source.ToString(), progress, cancelToken, (ex) =>
				{
					Error = ex.Message;
					Errored = true;
				});
			if (imageLoader != null) //minimum to identify the image type
            {
                loadedOne = true;
				SnooStreamViewModel.SystemServices.RunUIAsync(() =>
					{
						var madeImageVm = new ImageViewModel(this, source.ToString(), title, imageLoader);

						if (Images.Any(img => ((ImageViewModel)img).Url == source.ToString()))
						{
							//ignore duplicates (Shouldnt get here, need to investigate how it happens)		
						}
						else
						{
							Images.Add(madeImageVm);
						}
						return Task.FromResult(true);
					});
            }
            return loadedOne;
        }

        private IEnumerable<Tuple<string, string>> ApiResults { get; set; }
        public string Url { get; private set; }
        public string Domain { get; private set; }
        public int ApiImageCount { get; private set; }
        public ObservableCollection<ContentViewModel> Images { get; private set; }
        public string Title { get; private set; }

		protected override bool HasPreview
		{
			get
			{
				return ApiResults.Count() > 1;
			}
		}

		internal override async Task LoadContent(bool previewOnly, Action<int> progress, CancellationToken cancelToken)
        {
			if (previewOnly)
			{
				var firstImage = ApiResults.First();
				var addResult = await LoadImageImpl(firstImage.Item1, new Uri(firstImage.Item2), true, progress, cancelToken);
			}
			else
				await LoadAlbumImpl(progress, cancelToken);
        }
    }
}
