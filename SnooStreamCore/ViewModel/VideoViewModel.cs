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

        public IImageLoader Preview { get; private set; }
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
            var videoResult = await VideoAcquisition.GetPlayableStreams(Url, SnooStreamViewModel.SystemServices.SendGet);
            if (videoResult != null)
            {
                AvailableStreams = new ObservableCollection<Tuple<string, string>>(videoResult.PlayableStreams);
				if (AvailableStreams.Count > 0)
				{
					SelectedStream = AvailableStreams[0].Item1;
				}
				if (!string.IsNullOrWhiteSpace(videoResult.PreviewUrl))
				{
					var image = await SnooStreamViewModel.SystemServices.DownloadImageWithProgress(videoResult.PreviewUrl, progress, cancelToken);
					if (image != null)
					{
						Preview = image;
					}
				}
            }
        }
    }
}
