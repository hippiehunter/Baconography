using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class LoadingContentViewModel : ContentViewModel
    {
        public LoadingContentViewModel(ContentViewModel underlying) 
            : base(underlying.Context)
        {
            Underlying = underlying;
			IsInitiallyLoaded = false;
			underlying.BeginLoad(_cancelTokenSource.Token).ContinueWith((tsk) =>
				{
					IsInitiallyLoaded = true;
					RaisePropertyChanged("Underlying");
					RaisePropertyChanged("IsInitiallyLoaded");
				}, SnooStreamViewModel.UIScheduler);
        }

        public LoadingContentViewModel(Task<ContentViewModel> underlying, ViewModelBase context)
            : base(context)
        {
            var linkVm = context as LinkViewModel;
            underlying.ContinueWith(async (tsk) =>
                {
                    if (tsk.Status == TaskStatus.RanToCompletion)
                    {
						Underlying = tsk.Result;
						await Underlying.BeginLoad(_cancelTokenSource.Token);
                        IsInitiallyLoaded = true;
                        RaisePropertyChanged("Underlying");
                        RaisePropertyChanged("IsInitiallyLoaded");
                    }
                }, SnooStreamViewModel.UIScheduler);
        }

		private CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();
        public ContentViewModel Underlying { get; set; }
        public bool IsInitiallyLoaded { get; set; }

		internal override Task LoadContent(bool previewOnly, Action<int> progress, CancellationToken cancelToken)
        {
            if (Underlying != null)
            {
				return Underlying.LoadContent(previewOnly, progress, cancelToken);
            }
            return Task.FromResult<bool>(false);
        }
    }
}
