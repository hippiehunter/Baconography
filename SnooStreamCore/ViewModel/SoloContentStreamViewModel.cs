using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace SnooStream.ViewModel
{
    class SoloContentStreamViewModel : ViewModelBase, IHasLinks
    {

        public SoloContentStreamViewModel(LinkViewModel linkVM)
        {
            CurrentSelected = linkVM;
            Links = new ObservableCollection<ILinkViewModel>(new ILinkViewModel[] { linkVM });
        }


        public ILinkViewModel CurrentSelected { get; set; }

        public ObservableCollection<ILinkViewModel> Links { get; set; }
    }
}
