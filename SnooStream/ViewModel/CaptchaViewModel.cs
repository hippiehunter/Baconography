using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class CaptchaViewModel : ViewModelBase
    {
        public string CaptchaResponse { get; private set; }
        private float _loadPercent;
        public float LoadPercent
        {
            get
            {
                return _loadPercent;
            }
            set
            {
                if (_loadPercent != value)
                {
                    _loadPercent = value;
                    RaisePropertyChanged("LoadPercent");
                }
            }
        }
        public CaptchaViewModel(string iden)
        {
            LoadContent(iden);
        }

        private async void LoadContent(string iden)
        {
            var url = "http://www.reddit.com/captcha/" + iden;
            try
            {
                var bytes = await SnooStreamViewModel.SystemServices.DownloadWithProgress(url, (progress) => LoadPercent = progress, SnooStreamViewModel.UIContextCancellationToken);
                if (bytes != null) //minimum to identify the image type
                {
                    Content = new ImageViewModel(this, url, null, new Common.ImageSource(url, bytes), bytes);
                }
            }
            catch (Exception ex)
            {
                Content = new ErrorContentViewModel(this, ex);
            }
        }

        private ContentViewModel _content;
        public ContentViewModel Content
        {
            get
            {
                return _content;
            }
            set
            {
                _content = value;
                RaisePropertyChanged("Content");
            }
        }
    }
}
