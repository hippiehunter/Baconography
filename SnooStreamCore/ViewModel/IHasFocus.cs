using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public interface IHasFocus
    {
        ViewModelBase CurrentlyFocused { get; set; }
        //old and new view model
        event Action<ViewModelBase,ViewModelBase> FocusChanged;
    }
}
