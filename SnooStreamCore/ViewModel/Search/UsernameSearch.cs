using GalaSoft.MvvmLight;
using SnooStream.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel.Search
{
    public class UsernameSearch : ViewModelBase
    {
        static HashSet<string> _knownUsers = new HashSet<string>();
        public static void AddKnownUser(string username)
        {
            var lowerUser = username.ToLower();
            if (!_knownUsers.Contains(lowerUser))
                _knownUsers.Add(lowerUser);
        }

        public UsernameSearch()
        {
            SearchUserNames = new ObservableCollection<string>();
            _searchHelper = new SearchHelper(() => SearchUserNames.Clear(), (searchString) =>
                {
                    var lowerSearch = searchString.ToLower();
                    SearchUserNames.Clear();
                    foreach (var user in _knownUsers.Where(str => str.Contains(lowerSearch)))
                        SearchUserNames.Add(user);
                    
                }, 1, "~", 0);
        }


        SearchHelper _searchHelper;
        public ObservableCollection<string> SearchUserNames { get; set; }
        public string SearchString
        {
            get
            {
                return _searchHelper.SearchString;
            }
            set
            {
                _searchHelper.SearchString = value;
            }
        }
    }
}
