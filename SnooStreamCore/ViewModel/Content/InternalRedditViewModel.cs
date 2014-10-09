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
		private string url;

		public InternalRedditViewModel(string url)
		{
			// TODO: Complete member initialization
			this.url = url;
		}
	}
}
