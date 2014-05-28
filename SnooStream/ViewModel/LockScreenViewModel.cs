using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class LockScreenViewModel : ViewModelBase
    {

        string _selectedImage;
        public string SelectedImage
        {
            get
            {
                return _selectedImage;
            }
            set
            {
                _selectedImage = value;
                RaisePropertyChanged("SelectedImage");
            }
        }

    }
}
