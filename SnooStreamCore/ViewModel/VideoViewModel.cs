using CommonResourceAcquisition.VideoAcquisition;
using GalaSoft.MvvmLight;
using SnooStream.Common;
using SnooStream.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class VideoViewModel : ContentViewModel
    {
        public VideoViewModel(ViewModelBase context, string url) : base(context)
        {
            AvailableStreams = new ObservableCollection<Tuple<string, string>>();
            Url = url;
        }
        public ObservableCollection<Tuple<string, string>> AvailableStreams { get; private set; }

        public object Preview { get; private set; }
        public string Url { get; private set; }

        private string _selectedStream;
        public string SelectedStream
        {
            get
            {
                return _selectedStream;
            }
            set
            {
                _selectedStream = value;
                RaisePropertyChanged("SelectedStream");
            }
        }

		internal override async Task LoadContent(bool previewOnly, Action<int> progress, CancellationToken cancelToken)
        {
            var videoResult = VideoAcquisition.GetVideo(Url);
            if (videoResult != null)
            {
                AvailableStreams = new ObservableCollection<Tuple<string, string>>(await videoResult.PlayableStreams(cancelToken));
				if (AvailableStreams.Count > 0)
				{
					SelectedStream = AvailableStreams[0].Item1;
				}
				var previewResult = await videoResult.PreviewUrl(cancelToken);
				if (!string.IsNullOrWhiteSpace(previewResult))
				{
					var image = SnooStreamViewModel.SystemServices.DownloadImageWithProgress(previewResult, progress, cancelToken, (ex) =>
						{
							Errored = true;
							Error = ex.Message;
						});
					if (image != null)
					{
						Preview = image;
					}
				}
            }
        }
    }
}
