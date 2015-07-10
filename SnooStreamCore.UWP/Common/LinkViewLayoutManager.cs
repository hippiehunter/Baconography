using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using SnooStream.Messages;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.UI.Xaml;

namespace SnooStream.Common
{
    public class LinkViewLayoutManager : ObservableObject
    {
        const int PictureColumnWidth = 100;

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
            if (SnooStreamViewModel.Settings.LeftHandedMode != _leftHandedMode)
                LeftHandedMode = SnooStreamViewModel.Settings.LeftHandedMode;
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
