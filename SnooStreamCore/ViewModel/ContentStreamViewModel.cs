using GalaSoft.MvvmLight;
using SnooStream.ViewModel.Content;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
	public class ContentStreamViewModel : ViewModelBase
	{
		public static ObservableCollection<ILinkViewModel> MakeFilteredContentStream(ObservableCollection<ILinkViewModel> raw, ILinkViewModel hasImmunity)
		{
			var filterFunc = new Func<ILinkViewModel, bool>((link) =>
			{
				if (link == hasImmunity)
					return true;
				else if (SnooStreamViewModel.Settings.OnlyFlipViewUnread && SnooStreamViewModel.OfflineService.HasHistory(link.Url))
					return false;
				else if (link.Content is InternalRedditViewModel)
					return false;
				else if (SnooStreamViewModel.Settings.OnlyFlipViewImages)
				{
					if (link.Content is PlainWebViewModel || link.Content is SelfViewModel)
						return false;
				}

				return true;
			});

			ObservableCollection<ILinkViewModel> filteredCollection = new ObservableCollection<ILinkViewModel>(raw.Where(filterFunc));

			raw.CollectionChanged += (s, e) =>
				{
					switch (e.Action)
					{
						case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
							filteredCollection.Add(e.NewItems[0] as ILinkViewModel);
							break;
						case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
							filteredCollection.Move(e.OldStartingIndex, e.NewStartingIndex);
							break;
						case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
							filteredCollection.RemoveAt(e.OldStartingIndex);
							break;
						case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
							filteredCollection[e.NewStartingIndex] = e.NewItems[0] as ILinkViewModel;
							break;
						case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
							filteredCollection.Clear();
							break;
						default:
							break;
					}
				};
			return filteredCollection;
		}
	}
}
