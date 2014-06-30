using GalaSoft.MvvmLight;
using SnooSharp;
using SnooStream.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class UserHubViewModel : ViewModelBase
    {
        public UserHubViewModel(SelfInit initializationBlob)
        {
            Login = new LoginViewModel();
            if (initializationBlob != null)
                Self = new SelfViewModel(initializationBlob.SelfThings, initializationBlob.AfterSelfMessage, initializationBlob.AfterSelfSentMessage, initializationBlob.AfterSelfAction);
            else
                Self = new SelfViewModel(Enumerable.Empty<Thing>(), null, null, null);
        }
        public SelfViewModel Self {get; private set;}
        public LoginViewModel Login { get; private set; }
    }
}
