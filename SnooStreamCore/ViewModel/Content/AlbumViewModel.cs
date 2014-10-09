using CommonResourceAcquisition.ImageAcquisition;
using GalaSoft.MvvmLight;
using SnooStream.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel.Content
{
	public class AlbumViewModel : ContentViewModel
	{
		private string _url;
		private string _title;

		public string RedditThumbnail { get; private set; }
		Lazy<Task<IEnumerable<Tuple<string, string>>>> _apiResult;

		public ObservableCollection<ImageViewModel> Images { get; private set; }
		public AlbumViewModel(string url, string title, string linkThumbnailUrl)
		{
			_url = url;
			_title = title;
			RedditThumbnail = linkThumbnailUrl;
			_apiResult = new Lazy<Task<IEnumerable<Tuple<string, string>>>>(LoadAPI);
			Images = SnooStreamViewModel.SystemServices.MakeIncrementalLoadCollection<ImageViewModel>(new AlbumLoader(this));
		}

		private class AlbumLoader : IIncrementalCollectionLoader<ImageViewModel>
		{
			AlbumViewModel _viewModel;
			bool _hasLoaded = false;
			public AlbumLoader(AlbumViewModel viewModel)
			{
				_viewModel = viewModel;
			}
			public Task AuxiliaryItemLoader(IEnumerable<ImageViewModel> items, int timeout)
			{
				//nothing to load here
				return Task.FromResult(true);
			}

			public bool IsStale
			{
				//api results are never considered stale 
				get { return false; }
			}

			public bool HasMore()
			{
				return !_hasLoaded;
			}

			public async Task<IEnumerable<ImageViewModel>> LoadMore()
			{
				if (!_hasLoaded)
				{
					var apiResult = await _viewModel._apiResult.Value;
					return apiResult.Select(tpl => new ImageViewModel(tpl.Item2, tpl.Item1, null));
				}
				else
					return Enumerable.Empty<ImageViewModel>();
			}

			public Task Refresh(ObservableCollection<ImageViewModel> current, bool onlyNew)
			{
				throw new NotImplementedException();
			}
		}


		private async Task<IEnumerable<Tuple<string, string>>> LoadAPI()
		{
			return await ImageAcquisition.GetImagesFromUrl(_title, _url);
		}

		public async Task<string> FirstUrl()
		{
			var firstTpl = (await _apiResult.Value).FirstOrDefault();
			if (firstTpl != null)
			{
				return firstTpl.Item2;
			}
			else
				return RedditThumbnail;
		}
	}
}
