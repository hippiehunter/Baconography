using GalaSoft.MvvmLight.Messaging;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Messages
{
    class SubredditSelectedMessage : MessageBase
    {
        public LinkRiverViewModel ViewModel { get; set; }
    }
}
