using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SnooStream.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel.Popups
{
    public class InputViewModel : ViewModelBase
    {
        public InputViewModel(string prompt, string defaultInputValue, IEnumerable<string> options, RelayCommand<string> dismissed)
        {
            Options = new ObservableCollection<string>(options);
            SearchOptions = new ObservableCollection<string>();
            Dismissed = dismissed;
            SearchHelper = new SearchHelper(() =>
                {
                    SearchOptions.Clear();
                    foreach (var option in Options)
                        SearchOptions.Add(option);
                },
                (searchString) =>
                {
                    var lowerCaseSearch = searchString.ToLower();
                    var filteredOptions = Options.Where(str => str.ToLower().Contains(searchString) || lowerCaseSearch.Contains(str.ToLower())).ToList();
                    var existingSearch = SearchOptions.ToList();
                    foreach (var option in existingSearch)
                    {
                        if (filteredOptions.Contains(option))
                            filteredOptions.Remove(option);
                        else
                            SearchOptions.Remove(option);
                    }
                    foreach (var option in filteredOptions)
                        SearchOptions.Add(option);
                }, 1, "`", 0);
            Prompt = prompt;
            InputValue = defaultInputValue;
            //clear it so we dont start with any search populated
            SearchOptions.Clear();
            
        }
        private SearchHelper SearchHelper { get; set; }
        public string Prompt { get; set; }
        public string InputValue
        {
            get
            {
                return SearchHelper.SearchString;
            }
            set
            {
                SearchHelper.SearchString = value;
            }
        }
        public ObservableCollection<string> Options { get; set; }
        public ObservableCollection<string> SearchOptions { get; set; }
        public RelayCommand<string> Dismissed { get; set; }
    }
}
