using GalaSoft.MvvmLight.Messaging;
using SnooSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Messages
{
    class UserLoggedInMessage : MessageBase
    {
        public bool IsDefault { get; set; }
    }
}
