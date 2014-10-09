using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel.Content
{
	public class SelfViewModel : ContentViewModel
	{
		private LinkViewModel _selfLink;

		public SelfViewModel(LinkViewModel selfLink)
		{
			_selfLink = selfLink;
		}
	}
}
