﻿using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
            //LoadContent(iden);
        }

		//private async void LoadContent(string iden)
		//{
		//	var url = "http://www.reddit.com/captcha/" + iden;
		//	CancellationTokenSource tokenSource = new CancellationTokenSource();
		//	var imageLoader = SnooStreamViewModel.SystemServices.DownloadImageWithProgress(url, (progress) => { }, tokenSource.Token, (ex) =>
		//	{
		//		if (Content != null)
		//		{
		//			Content.Errored = true;
		//			Content.Error = ex.Message;
		//		}
				
		//	});
		//	Content = new ImageViewModel(this, url, null, imageLoader);
		//	await Content.BeginLoad(SnooStreamViewModel.UIContextCancellationToken);   
		//}

		//private ContentViewModel _content;
		//public ContentViewModel Content
		//{
		//	get
		//	{
		//		return _content;
		//	}
		//	set
		//	{
		//		_content = value;
		//		RaisePropertyChanged("Content");
		//	}
		//}
    }
}
