using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel.Content
{
	public class VideoViewModel : ContentViewModel
	{
		private string _url;

		public VideoViewModel(string url)
		{
			_url = url;
		}

		internal Task<string> StillUrl()
		{
			throw new NotImplementedException();
			//CommonResourceAcquisition.VideoAcquisition.
		}
	}
}
