using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class ErrorContentViewModel : ContentViewModel
    {
        public ErrorContentViewModel(ViewModelBase context, Exception ex) : base(context)
        {
            Error = ex.ToString();
        }

        
        public string Error { get; private set; }
		internal override async Task LoadContent(bool previewOnly, Action<int> progress, CancellationToken cancelToken)
        {
            //nothing to load here
        }
    }
}
