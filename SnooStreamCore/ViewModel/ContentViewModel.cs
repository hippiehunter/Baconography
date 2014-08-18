using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnooStream.Common;
using System.Threading;

namespace SnooStream.ViewModel
{
    public abstract class ContentViewModel : ViewModelBase
    {
        public ContentViewModel(ViewModelBase context)
        {
            Context = context;
			if (Context is LinkStreamViewModel)
			{
				Context = null;
			}
            var contextLink = Context as LinkViewModel;
            if (contextLink != null)
            {
                PreviewText = NBoilerpipePortable.Util.HttpUtility.HtmlDecode(contextLink.Title).Replace("\t", "").Replace("\n", "");
                if (!string.IsNullOrWhiteSpace(contextLink.Thumbnail))
                    PreviewImage = contextLink.Thumbnail;

				LoadContextToken = contextLink.Url;
            }
        }

		public Task BeginLoad(CancellationToken cancelToken)
        {
            if (ContentLoadTask == null)
            {
                lock (this)
                {
                    if (ContentLoadTask == null)
                    {
                        Loading = true;
						ContentLoadTask = SnooStreamViewModel.NotificationService.ReportWithProgress(LoadText, async (report) =>
							{
								try
								{
									if (HasPreview)
									{
										await SnooStreamViewModel.LoadQueue.QueueLoadItem(LoadContextToken, LoadContextType.Minor, async () =>
											{
												var error = await ErrorControlledLoadContent(true, (progress) => report(PreviewLoadPercent = progress), cancelToken);
												if (error != null)
												{
													Errored = true;
													Error = error.ToString();
												}
											});
									}

									var mainLoadTask = SnooStreamViewModel.LoadQueue.QueueLoadItem(LoadContextToken, LoadContextType.Minor, async () =>
									{
										var error = await ErrorControlledLoadContent(false, !HasPreview ? report : (progress) => report(PreviewLoadPercent = progress), cancelToken);
										if (error != null)
										{
											Errored = true;
											Error = error.ToString();
										}
									});

									if (!HasPreview)
										await mainLoadTask;

								}
								catch (Exception ex)
								{
									Errored = true;
									Error = ex.ToString();
								}
							});
						SnooStreamViewModel.SystemServices.RunUIAsync(async () =>
							{
								try
								{
									await ContentLoadTask;
									Loaded = true;
									Loading = false;

									RaisePropertyChanged("Loaded");
									RaisePropertyChanged("Loading");
									RaisePropertyChanged("Content");
								}
								catch(Exception ex)
								{
									Errored = true;
									Error = ex.ToString();
									RaisePropertyChanged("Errored");
									RaisePropertyChanged("Error");
								}
							});
                    }
                }
            }
            return ContentLoadTask;
        }

        Task ContentLoadTask { get; set; }
		internal abstract Task LoadContent(bool previewOnly, Action<int> progress, CancellationToken cancelToken);
		internal async Task<Exception> ErrorControlledLoadContent(bool previewOnly, Action<int> progress, CancellationToken cancelToken)
		{
			try
			{
				cancelToken.ThrowIfCancellationRequested();
				await LoadContent(previewOnly, progress, cancelToken);
			}
			catch (Exception ex)
			{
				return ex;
			}
			return null;
		}

		protected virtual bool HasPreview { get { return false; } }

        public ViewModelBase Context { get; private set; }
        public bool Loaded { get; set; }
        public bool Loading { get; set; }
		private int _previewLoadPercent = 0;
		public int PreviewLoadPercent
		{
			get
			{
				return _previewLoadPercent;
			}
			set
			{
				_previewLoadPercent = value;
				SnooStreamViewModel.SystemServices.QueueNonCriticalUI(() => RaisePropertyChanged("PreviewLoadPercent"));
			}
		}
        public string PreviewText { get; set; }
        public object PreviewImage { get; set; } //might be a url, or might be a previewImageSource
        public string Error { get; set; }
        public bool Errored { get; set; }
		public string LoadContextToken { get; set; }
		public string LoadText { get; set; }

    }
}
