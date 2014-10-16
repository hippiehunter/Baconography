using GalaSoft.MvvmLight;
using NBoilerpipePortable.Extractors;
using NBoilerpipePortable.Util;
using SnooStream.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel.Content
{
	public class Readable : ViewModelBase
	{
	}

	public class ReadableText : Readable
	{
		public string Text { get; set; }
	}

	public class ReadableImage : Readable
	{
		public string Url { get; set; }
	}
	public class PlainWebViewModel : ContentViewModel
	{
		public bool NoPreview { get; private set; }
		public bool TextPreview { get; private set; }
		public bool ImagePreview { get; private set; }
		public bool NotText { get; private set; }
		public string Url { get; private set; }
		public string Title { get; private set; }
		public string RedditThumbnail { get; private set; }
		public ObservableCollection<Readable> WebParts { get; private set; }
		Lazy<Task<Tuple<string, string, IEnumerable<Readable>>>> _firstResult;
		private string _nextUrl;
		static HttpClient _httpClient;

		static PlainWebViewModel()
		{
			var handler = new HttpClientHandler();
			if (handler.SupportsAutomaticDecompression)
			{
				handler.AutomaticDecompression = DecompressionMethods.GZip |
												 DecompressionMethods.Deflate;
			}
			_httpClient = new HttpClient(handler);
		}

		public PlainWebViewModel(bool notText, string url, string redditThumbnail)
        {
            TextPreview = !notText;
            Url = url;
			RedditThumbnail = redditThumbnail;
			_firstResult = new Lazy<Task<Tuple<string, string, IEnumerable<Readable>>>>(() => LoadOneImpl(_httpClient, url));
			WebParts = SnooStreamViewModel.SystemServices.MakeIncrementalLoadCollection(new WebLoader(this));
        }

		WeakReference<string> _weakPageData;
		Task<string> _httpLoad;
		private async Task<string> GetPageData(string url)
		{
			if (_weakPageData != null)
			{
				string pageData;
				if (_weakPageData.TryGetTarget(out pageData))
					return pageData;
			}
			if (_httpLoad != null)
			{
				return await _httpLoad;
			}
			else
			{
				var blobRetrive = await SnooStreamViewModel.OfflineService.RetriveBlob<string>(url, TimeSpan.FromDays(5));
				if (blobRetrive != null)
				{
					_weakPageData = new WeakReference<string>(blobRetrive);
					return blobRetrive;
				}
				else
				{
					_httpLoad = _httpClient.GetStringAsync(url);
					var result = await _httpLoad;
					_weakPageData = new WeakReference<string>(result);
					await SnooStreamViewModel.OfflineService.StoreBlob(url, result);
					_httpLoad = null;
					return result;
				}
			}
		}
		

		private class WebLoader : IIncrementalCollectionLoader<Readable>
		{
			PlainWebViewModel _viewModel;
			bool _hasLoaded = false;
			public WebLoader(PlainWebViewModel viewModel)
			{
				_viewModel = viewModel;
			}
			public Task AuxiliaryItemLoader(IEnumerable<Readable> items, int timeout)
			{
				//nothing to load here
				return Task.FromResult(true);
			}

			public bool IsStale
			{
				//web results are never considered stale 
				get { return false; }
			}

			public bool HasMore()
			{
				return !_hasLoaded || !string.IsNullOrEmpty(_viewModel._nextUrl);
			}

			public async Task<IEnumerable<Readable>> LoadMore()
			{
				if (!_hasLoaded || !string.IsNullOrEmpty(_viewModel._nextUrl))
				{
					var loadResult = await Task.Run(() => _viewModel.LoadOneImpl(_httpClient, _viewModel._nextUrl ?? _viewModel.Url));
					_viewModel._nextUrl = loadResult.Item1;
					_hasLoaded = true;
					return loadResult.Item3;
				}
				else
					return Enumerable.Empty<Readable>();
			}

			public Task Refresh(ObservableCollection<Readable> current, bool onlyNew)
			{
				throw new NotImplementedException();
			}


			public string NameForStatus
			{
				get { return "readable part"; }
			}
		}

		private async Task<Tuple<string, string, IEnumerable<Readable>>> LoadOneImpl(HttpClient httpClient, string url)
		{
			try
			{
				var result = new List<Readable>();
				string domain = url;
				if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
					domain = new Uri(url).Authority;

				var page = await GetPageData(url);
				string title;
				var pageBlocks = ArticleExtractor.INSTANCE.GetTextAndImageBlocks(page, new Uri(url), out title);
				foreach (var tpl in pageBlocks)
				{
					if (!string.IsNullOrEmpty(tpl.Item2))
					{
						result.Add(new ReadableImage { Url = tpl.Item2 });
					}

					if (!string.IsNullOrEmpty(tpl.Item1))
					{
						result.Add(new ReadableText { Text = tpl.Item1 });
					}
				}
				var nextPageUrl = MultiPageUtils.FindNextPageLink(SgmlDomBuilder.GetBody(SgmlDomBuilder.BuildDocument(page)), url);
				return Tuple.Create(nextPageUrl, title, (IEnumerable<Readable>)result);
			}
			catch (Exception ex)
			{
				this.SetErrorStatus(ex.Message);
				return Tuple.Create("", "", Enumerable.Empty<Readable>());
			}
		}

		internal async Task<string> FirstParagraph()
		{
			var onePageResult = await _firstResult.Value;
			var result = onePageResult.Item3.FirstOrDefault((rd) => rd is ReadableText) as ReadableText;
			if(result != null)
			{
				return result.Text;
			}
			return onePageResult.Item2;
		}

        internal async Task<string> FirstImage()
        {
			var onePageResult = await _firstResult.Value;
            var result = onePageResult.Item3.FirstOrDefault((rd) => rd is ReadableImage) as ReadableImage;
            if (result != null)
            {
                return result.Url;
            }
            return null;
        }

		protected override Task StartLoad()
		{
			//TODO maybe this should do a full load, not sure
			return Task.Run(() => FirstParagraph());
		}
	}
}
