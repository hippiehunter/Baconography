using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel.Content
{
	public class PlainWebViewModel : ContentViewModel
	{
		public bool NoPreview { get; private set; }
        public bool TextPreview { get; private set; }
        public bool ImagePreview { get; private set; }
        public bool NotText { get; private set; }
        public string Url { get; private set; }
        public string Title { get; private set; }
		public string RedditThumbnail { get; private set; }
        public ObservableCollection<object> WebParts { get; private set; }

		public PlainWebViewModel(bool notText, string url, string redditThumbnail)
        {
            TextPreview = !notText;
            Url = url;
			RedditThumbnail = redditThumbnail;
            WebParts = new ObservableCollection<object>();
        }
	}
}
