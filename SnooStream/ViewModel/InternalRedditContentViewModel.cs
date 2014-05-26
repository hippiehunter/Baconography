using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class InternalRedditContentViewModel : ContentViewModel
    {
        public InternalRedditContentViewModel(ViewModelBase context, string url) : base(context)
        {

        }

		internal override async Task LoadContent(bool previewOnly, Action<int> progress, CancellationToken cancelToken)
        {
            //nothing to load here
        }
    }
}
