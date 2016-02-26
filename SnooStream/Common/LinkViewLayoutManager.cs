using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using SnooStream.ViewModel;
using SnooStream.ViewModel.Messages;
using Windows.UI.Xaml;

namespace SnooStream.Common
{
    public class LinkViewLayoutManager : SnooObservableObject
    {
        const int PictureColumnWidth = 100;
        public ISettingsContext Settings { get; set; }
        public LinkViewLayoutManager()
        {
            FirstColumnWidth = new GridLength(1, GridUnitType.Star);
            SecondColumnWidth = new GridLength(PictureColumnWidth, GridUnitType.Pixel);
            PictureColumn = 1;
            TextColumn = 0;
            Messenger.Default.Register<SettingsChangedMessage>(this, OnSettingsChanged);
        }

        private void OnSettingsChanged(SettingsChangedMessage message)
        {
            var settingsVM = new SettingsViewModel(Settings);
            if (settingsVM.LeftHandedMode != _leftHandedMode)
                LeftHandedMode = settingsVM.LeftHandedMode;
        }

        private bool _leftHandedMode;
        public bool LeftHandedMode
        {
            get
            {
                return _leftHandedMode;
            }
            set
            {
                _leftHandedMode = value;
                if (value)
                {
                    FirstColumnWidth = new GridLength(PictureColumnWidth, GridUnitType.Pixel);
                    SecondColumnWidth = new GridLength(1, GridUnitType.Star);
                    PictureColumn = 0;
                    TextColumn = 1;
                }
                else
                {
                    FirstColumnWidth = new GridLength(1, GridUnitType.Star);
                    SecondColumnWidth = new GridLength(PictureColumnWidth, GridUnitType.Pixel);
                    PictureColumn = 1;
                    TextColumn = 0;
                }
                RaisePropertyChanged("LeftHandedMode");
                RaisePropertyChanged("FirstColumnWidth");
                RaisePropertyChanged("SecondColumnWidth");
                RaisePropertyChanged("PictureColumn");
                RaisePropertyChanged("TextColumn");
            }
        }

        public GridLength FirstColumnWidth
        {
            get;
            private set;
        }

        public GridLength SecondColumnWidth
        {
            get;
            private set;
        }

        public int PictureColumn
        {
            get;
            private set;
        }

        public int TextColumn
        {
            get;
            private set;
        }
    }
}
