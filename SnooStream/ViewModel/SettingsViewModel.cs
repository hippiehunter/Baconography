using GalaSoft.MvvmLight;
using SnooStream.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class SettingsViewModel : ViewModelBase
    {
        public PrimaryLiveTileViewModel PrimaryLiveTile { get; set; }
        public SecondaryLiveTileHubViewModel SecondaryLiveTileHub { get; set; }
        public LockScreenViewModel LockScreen { get; set; }
        public AppearanceSettingsViewModel LayoutSettings { get; set; }
        public ContentSettingsViewModel ContentSettings { get; set; }
        public Settings Settings { get; set; }
    }
}
