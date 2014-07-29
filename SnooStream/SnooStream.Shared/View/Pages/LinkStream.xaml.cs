using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using SnooStream.ViewModel;
using System.Collections.ObjectModel;
using SnooStream.Common;
using System.Threading.Tasks;
using System.Diagnostics;
using SnooStream.View.Controls;
using System.ComponentModel;
using Windows.UI.Input;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media;
using SnooStream.View.Selectors;

namespace SnooStream.View.Pages
{
	public partial class LinkStream : SnooApplicationPage, INotifyPropertyChanged
	{
		public LinkStream()
		{
			ManipulationController = new ManipulationController();
			InitializeComponent();
		}

		bool _noMoreLoad = false;
		bool _noMoreLoadBack = false;
		ObservableCollection<ContentViewModel> _links;
		public ManipulationController ManipulationController { get; set; }
		public ObservableCollection<ContentViewModel> Links
		{
			get
			{
				if (_links == null)
				{
					flipView.SelectionChanged += flipViewIndexChanged;
					_links = new ObservableCollection<ContentViewModel>();
					LoadInitialLinks(_links);
				}
				return _links;
			}
		}

		public ContentViewModel SelectedItem
		{
			get { return ((LinkStreamViewModel)DataContext).Visible; }
			set { ((LinkStreamViewModel)DataContext).Visible = value; }
		}

		bool _loading = false;

		public async void LoadInitialLinks(ObservableCollection<ContentViewModel> links)
		{
			try
			{
				_loading = true;
				var linkStream = DataContext as LinkStreamViewModel;
				if (linkStream != null && await linkStream.MoveNext())
				{
					AddLoadingLink(links, linkStream.Current, false);
					for (int i = 0; i < 5 && await linkStream.MoveNext(); i++)
					{
						AddLoadingLink(links, linkStream.Current, false);
					}
					var first = links[0];
					SelectedItem = first;
					if (PropertyChanged != null)
						PropertyChanged(this, new PropertyChangedEventArgs("SelectedItem"));

					await Task.Delay(500);
					var backLinkCount = 0;
					LinkViewModel currentPrior;
					while (linkStream.LoadPrior.Value != null &&
						(currentPrior = (await linkStream.LoadPrior.Value.Next()) as LinkViewModel) != null &&
						backLinkCount < 5)
					{
						AddLoadingLink(links, currentPrior, true);
					}

					SelectedItem = first;

					if (PropertyChanged != null)
						PropertyChanged(this, new PropertyChangedEventArgs("SelectedItem"));
				}
			}
			finally
			{
				_loading = false;
			}
		}

		private void AddLoadingLink(ObservableCollection<ContentViewModel> links, LinkViewModel link, bool first)
		{
			if (link == null)
			{
				Debug.WriteLine("something tried to add a null link for loading content");
				return;
			}
			LoadingContentViewModel loadingVM;
			if (link.Content != null)
			{
				loadingVM = new LoadingContentViewModel(link.Content);
			}
			else
			{
				loadingVM = new LoadingContentViewModel(link.AsyncContent, link != null ? link : DataContext as GalaSoft.MvvmLight.ViewModelBase);
			}

			if (first)
			{
				links.Insert(0, loadingVM);
			}
			else
			{
				links.Add(loadingVM);
			}
		}

		private async void flipViewIndexChanged(object sender, SelectionChangedEventArgs e)
		{
			if (SelectedItem != null)
			{
				SnooStreamViewModel.LoadQueue.SetPrimaryLoadContext(SelectedItem.LoadContextToken);
			}

			if (Links.Count > 0 && !_loading)
			{
				try
				{
					_loading = true;
					//preload distance
					if (!_noMoreLoad && flipView.SelectedIndex > (Links.Count - 5))
					{
						_noMoreLoad = !(await ((LinkStreamViewModel)DataContext).MoveNext());
						if (!_noMoreLoad)
						{
							AddLoadingLink(Links, ((LinkStreamViewModel)DataContext).Current, false);
						}
					}
					if (!_noMoreLoadBack && flipView.SelectedIndex <= 5)
					{
						var backEnum = ((LinkStreamViewModel)DataContext).LoadPrior.Value;
						var currentPrior = await backEnum.Next() as LinkViewModel;
						_noMoreLoadBack = currentPrior == null;
						if (!_noMoreLoadBack)
						{
							AddLoadingLink(Links, currentPrior, true);
						}
					}
				}
				finally
				{
					_loading = false;
				}
			}
		}

		private void flipView_DoubleTap(object sender, DoubleTappedRoutedEventArgs e)
		{
			ManipulationController.FireDoubleTap(sender, e);
		}

		private void flipView_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			ManipulationController.FireManipulationStarted(sender, e);
		}

		private void flipView_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			ManipulationController.FireManipulationCompleted(sender, e);
		}

		private void flipView_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			ManipulationController.FireManipulationDelta(sender, e);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private Image FindImageInTemplates(UIElement element)
		{
			if (element is FlipViewItem)
			{
				return FindImageInTemplates(((FlipViewItem)element).ContentTemplateRoot);
			}
			else if (element is Grid)
			{
				return FindImageInTemplates(((Grid)element).Children[1]);
			}
			else if (element is ContentControl)
			{
				return FindImageInTemplates(((ContentControl)element).ContentTemplateRoot);
			}
			else if (element is Image)
				return element as Image;
			else
				return null;
		}
	}
}