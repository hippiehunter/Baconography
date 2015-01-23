using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel.Popups
{
    public class InputViewModel : ViewModelBase
    {
        public string Prompt { get; set; }
        public string InputValue { get; set; }
        public RelayCommand<string> Dismissed { get; set; }
    }
}
