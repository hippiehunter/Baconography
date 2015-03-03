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
    public class OperationCancellationViewModel : ViewModelBase
    {
        public OperationCancellationViewModel(string message, CancellationTokenSource cancelSource)
        {
            Message = message;
            CancelSource = cancelSource;
        }
        public string Message { get; set; }
        public CancellationTokenSource CancelSource { get; set; }
        public RelayCommand Cancel
        {
            get
            {
                return new RelayCommand(() => CancelSource.Cancel());
            }
        }
    }
}
