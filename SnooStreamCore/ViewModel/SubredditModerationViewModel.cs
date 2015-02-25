using GalaSoft.MvvmLight;
using SnooSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class SubredditModerationViewModel : ViewModelBase
    {
        public Subreddit Thing {get; set;}
        public SubredditModerationViewModel(Subreddit thing)
        {
            Thing = thing;
        }
    }
}
