using GalaSoft.MvvmLight;
using SnooStream.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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

        private async void LoadAlbumImpl()
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
						if (await LoadImageImpl(tpl.Item1, imageUri, false))
							i++;
					}
					else
						i++;
                }
                
            }
        }
        private async Task<bool> LoadImageImpl(string title, Uri source, bool isPreview)
        {
            bool loadedOne = false;
            await SnooStreamViewModel.NotificationService.ReportWithProgress("loading from " + source.Host,
                async (report) =>
                {
                    var bytes = await SnooStreamViewModel.SystemServices.DownloadWithProgress(source.ToString(),
                        isPreview ? (progress) => report(PreviewLoadPercent = progress) : report, 
                        SnooStreamViewModel.UIContextCancellationToken);
                    if (bytes != null && bytes.Length > 6) //minimum to identify the image type
                    {
                        loadedOne = true;
						await Task.Factory.StartNew(() =>
                        {
							Images.Add(new ImageViewModel(this, source.ToString(), title, new ImageSource(source.ToString(), bytes), bytes));
						}, SnooStreamViewModel.UIContextCancellationToken, TaskCreationOptions.None, SnooStreamViewModel.UIScheduler);	
                    }
                });
            return loadedOne;
        }

        private IEnumerable<Tuple<string, string>> ApiResults { get; set; }
        public string Url { get; private set; }
        public string Domain { get; private set; }
        public int ApiImageCount { get; private set; }
        public ObservableCollection<ContentViewModel> Images { get; private set; }
        public string Title { get; private set; }
        public PreviewImageSource Preview { get; private set; }

        internal override async Task LoadContent()
        {
            var firstImage = ApiResults.First();
            var addResult = await LoadImageImpl(firstImage.Item1, new Uri(firstImage.Item2), true);
            if (addResult)
            {
                //Preview = Images.First().Preview;
            }
            LoadAlbumImpl();
        }
    }
}
