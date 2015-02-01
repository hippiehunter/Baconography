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
	public class ContentStreamViewModel
	{
        private static async void DelayContentStreamAddition(IEnumerable<ILinkViewModel> rawBefore, ObservableCollection<ILinkViewModel> target)
        {
            await Task.Delay(100);
            foreach (var raw in rawBefore.Reverse())
            {
                target.Insert(0, raw);
            }
        }
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

            var filteredList = raw.Where(filterFunc).ToList();
            var selectedIndex = filteredList.IndexOf(hasImmunity);
            ObservableCollection<ILinkViewModel> filteredCollection = new ObservableCollection<ILinkViewModel>(filteredList.Skip(selectedIndex));
            if(selectedIndex > 0)
            {
                var rawBefore = filteredList.Take(selectedIndex - 1);
                DelayContentStreamAddition(rawBefore, filteredCollection);
            }

			raw.CollectionChanged += (s, e) =>
				{
					switch (e.Action)
					{
						case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                            if (filterFunc(e.NewItems[0] as ILinkViewModel))
							    filteredCollection.Add(e.NewItems[0] as ILinkViewModel);
							break;
						case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                            if (filteredCollection.Contains(e.OldItems[0] as ILinkViewModel) &&
                                filteredCollection.Contains(e.NewItems[0] as ILinkViewModel))
                            {
                                filteredCollection.Move(filteredCollection.IndexOf(e.OldItems[0] as ILinkViewModel),
                                    filteredCollection.IndexOf(e.NewItems[0] as ILinkViewModel));
                            }
							break;
						case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                            if (filteredCollection.Contains(e.OldItems[0] as ILinkViewModel))
                            {
                                filteredCollection.Remove(e.OldItems[0] as ILinkViewModel);
                            }
							break;
						case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                            if (filterFunc(e.NewItems[0] as ILinkViewModel))
                            {
                                filteredCollection[filteredCollection.IndexOf(e.OldItems[0] as ILinkViewModel)] = e.NewItems[0] as ILinkViewModel;
                            }
                            else if (filteredCollection.Contains(e.OldItems[0] as ILinkViewModel))
                            {
                                filteredCollection.Remove(e.OldItems[0] as ILinkViewModel);
                            }
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
