using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel.Search
{
    public class SubredditSearch : ViewModelBase
    {
        ObservableCollection<string> SearchSubreddits { get; set; }
    }
}
