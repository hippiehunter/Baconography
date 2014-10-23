using SnooStream.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
	public interface IHasLinks
	{
		ObservableCollection<ILinkViewModel> Links { get; set; }
		IWrappedCollectionViewSource LinksViewSource { get; }
	}
}
