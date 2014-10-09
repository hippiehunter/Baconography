using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel.Content
{
	public class InternalRedditViewModel : ContentViewModel
	{
		private string _url;

		public InternalRedditViewModel(string url)
		{
			_url = url;
		}

		protected override Task StartLoad()
		{
			//TODO ??
			return Task.FromResult(true);
		}
	}
}
