using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnooStream.Common;

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
            }
        }

        public Task BeginLoad(bool highPriority)
        {
            if (ContentLoadTask == null)
            {
                lock (this)
                {
                    if (ContentLoadTask == null)
                    {
                        Loading = true;
                        ContentLoadTask = LoadContent().ContinueWith((tsk) => 
                            {
                                var tskResult = tsk.WasSuccessfull();
                                if (tskResult)
                                {
                                    Loaded = true;
                                    Loading = false;

                                    RaisePropertyChanged("Loaded");
                                    RaisePropertyChanged("Loading");
                                    RaisePropertyChanged("Content");
                                }
                                else
                                {
                                    Errored = true;
                                    Error = tsk.Exception.ToString();
                                    RaisePropertyChanged("Errored");
                                    RaisePropertyChanged("Error");
                                }
                            }, SnooStreamViewModel.UIScheduler);
                    }
                }
            }
            return ContentLoadTask;
        }

        public async Task BeginPreviewLoad()
        {
            if (SnooStreamViewModel.Settings.HeavyPreview)
            {
                await BeginLoad(false);
            }
            else if (Loaded || Loading)
            {
                await ContentLoadTask;
            }
        }

        Task ContentLoadTask { get; set; }
        internal abstract Task LoadContent();
        public ViewModelBase Context { get; private set; }
        public bool Loaded { get; set; }
        public bool Loading { get; set; }
        public int PreviewLoadPercent { get; set; }
        public string PreviewText { get; set; }
        public object PreviewImage { get; set; } //might be a url, or might be a previewImageSource
        public string Error { get; set; }
        public bool Errored { get; set; }
		public string LoadContextToken { get; set; }

    }
}
