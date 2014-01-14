﻿using CommonVideoAquisition;
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
    public class VideoViewModel : ContentViewModel
    {
        public VideoViewModel(ViewModelBase context, string url) : base(context)
        {
            AvailableStreams = new ObservableCollection<Tuple<string, string>>();
            Url = url;
        }
        public ObservableCollection<Tuple<string, string>> AvailableStreams { get; private set; }

        public ImageSource Preview { get; private set; }
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

        protected override async Task LoadContent()
        {
            var videoResult = await VideoAquisition.GetPlayableStreams(Url, SnooStreamViewModel.SystemServices.SendGet);
            AvailableStreams = new ObservableCollection<Tuple<string,string>>(videoResult.PlayableStreams);
            await SnooStreamViewModel.NotificationService.ReportWithProgress("loading from youtube",
                async (report) =>
                {
                    var bytes = await SnooStreamViewModel.SystemServices.DownloadWithProgress(Url, (progress) => report(PreviewLoadPercent = progress), SnooStreamViewModel.UIContextCancellationToken);
                    if (bytes != null && bytes.Length > 6) //minimum to identify the image type
                    {
                        Preview = new ImageSource(Url, bytes);
                    }
                });
        }
    }
}
