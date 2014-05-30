using GalaSoft.MvvmLight;
using SnooStream.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class LockScreenViewModel : ViewModelBase
    {
        Settings _settings;
        public LockScreenViewModel(Settings settings)
        {
            _settings = settings;
        }

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

        public bool UseLockScreenOverlay
        {
            get
            {
                return _settings.LockScreenOverlay;
            }
            set
            {
                _settings.LockScreenOverlay = value;
                RaisePropertyChanged("UseLockScreenOverlay");
            }
        }

        public bool MessagesInLockScreenOverlay
        {
            get
            {
                return _settings.MessagesInLockScreenOverlay;
            }
            set
            {
                _settings.MessagesInLockScreenOverlay = value;
                RaisePropertyChanged("MessagesInLockScreenOverlay");
            }
        }

        public int OverlayItemCount
        {
            get
            {
                return _settings.OverlayItemCount;
            }
            set
            {
                _settings.OverlayItemCount = value;
                RaisePropertyChanged("OverlayItemCount");
            }
        }

        public IEnumerable<int> ItemQuantityOptions
        {
            get
            {
                return new List<int> { 0, 1, 2, 3, 4, 5, 6 };
            }
        }

        public bool PostsInLockScreenOverlay
        {
            get
            {
                return _settings.PostsInLockScreenOverlay;
            }
            set
            {
                _settings.PostsInLockScreenOverlay = value;
                RaisePropertyChanged("PostsInLockScreenOverlay");
            }
        }

        public bool RoundedLockScreen
        {
            get
            {
                return _settings.RoundedLockScreen;
            }
            set
            {
                _settings.RoundedLockScreen = value;
                RaisePropertyChanged("RoundedLockScreen");
            }
        }

        public float OverlayOpacity
        {
            get
            {
                return _settings.OverlayOpacity / 100.0f;
            }
            set
            {
                _settings.OverlayOpacity = (int)(value * 100);
                RaisePropertyChanged("OverlayOpacity");
            }
        }
    }
}
