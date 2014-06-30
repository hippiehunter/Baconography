using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class AboutUserViewModel : ViewModelBase
    {
        private SnooSharp.TypedThing<SnooSharp.Account> account;

        public AboutUserViewModel(SnooSharp.TypedThing<SnooSharp.Account> account)
        {
            // TODO: Complete member initialization
            this.account = account;
        }
    }
}
