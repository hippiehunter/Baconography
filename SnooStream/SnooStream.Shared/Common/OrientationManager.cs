using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public class OrientationManager : ViewModelBase
    {
        private bool _isLandscape;
        public bool IsLandscape
        {
            get
            {
                return _isLandscape;
            }
            set
            {
                _isLandscape = value;
                RaisePropertyChanged("IsLandscape");
            }
        }
    }
}
