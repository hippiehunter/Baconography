using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel.Popups
{
    public class CommandViewModel : ViewModelBase
    {
        public class CommandItem
        {
            public string DisplayText { get; set; }
            public string Id { get; set; }
            public RelayCommand Command { get; set; }
        }
        public string Prompt { get; set; }
        public List<CommandItem> Commands { get; set; }
    }
}
