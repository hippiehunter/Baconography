using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class ConversationViewModel : ViewModelBase
    {
        SelfStreamViewModel _selfStream;
        public ConversationViewModel(ActivityGroupViewModel targetGroup, SelfStreamViewModel selfStream)
        {
            _selfStream = selfStream;
            CurrentGroup = targetGroup;
            GotoReply = new RelayCommand(() =>
            {

            });

            DeleteMessage = new RelayCommand(() =>
            {

            });

            GotoNewer = new RelayCommand(() =>
            {

            });

            GotoOlder = new RelayCommand(() =>
            {

            });
        }

        public ActivityGroupViewModel CurrentGroup { get; set; }


        public RelayCommand GotoReply { get; set; }
        public RelayCommand DeleteMessage { get; set; }
        public RelayCommand GotoNewer { get; set; }
        public RelayCommand GotoOlder { get; set; }
    }
}
