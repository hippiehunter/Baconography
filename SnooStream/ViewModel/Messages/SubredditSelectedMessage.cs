using GalaSoft.MvvmLight.Messaging;
using SnooSharp;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel.Messages
{
    class SubredditSelectedMessage : MessageBase
    {
        public Subreddit Subreddit { get; set; }
    }
}
