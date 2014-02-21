﻿using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class LoadingContentViewModel : ContentViewModel
    {
        public LoadingContentViewModel(ContentViewModel underlying) 
            : base(underlying.Context)
        {
            Underlying = underlying;
            IsInitiallyLoaded = true;
        }

        public LoadingContentViewModel(Task<ContentViewModel> underlying, ViewModelBase context)
            : base(context)
        {
            underlying.ContinueWith((tsk) =>
                {
                    if (tsk.Status == TaskStatus.RanToCompletion)
                    {
                        Underlying = tsk.Result;
                        IsInitiallyLoaded = true;
                        RaisePropertyChanged("Underlying");
                        RaisePropertyChanged("IsInitiallyLoaded");
                    }
                }, SnooStreamViewModel.UIScheduler);
        }

        public ContentViewModel Underlying { get; set; }
        public bool IsInitiallyLoaded { get; set; }

        internal override Task LoadContent()
        {
            if (Underlying != null)
            {
                return Underlying.LoadContent();
            }
            return Task.FromResult<bool>(false);
        }
    }
}
