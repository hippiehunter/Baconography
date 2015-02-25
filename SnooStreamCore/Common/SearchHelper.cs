using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public class SearchHelper
    {
        Action _defaultResults;
        Action<string> _startSearch;
        int _minimumCharCount;
        int _secondsBeforeSearch;
        string _alwaysSearchIfContains;

        public SearchHelper(Action defaultResults, Action<string> startSearch, int minimumCharCount, string alwaysSearchIfContains, int secondsBeforeSearch = 1)
        {
            _defaultResults = defaultResults;
            _startSearch = startSearch;
            _minimumCharCount = minimumCharCount;
            _alwaysSearchIfContains = alwaysSearchIfContains;
            _secondsBeforeSearch = secondsBeforeSearch;
        }

        private string _searchString;
        public string SearchString
        {
            get
            {
                return _searchString;
            }
            set
            {
                bool wasChanged = _searchString != value;
                if (wasChanged)
                {
                    _searchString = value;

                    if (_searchString.Length < _minimumCharCount)
                    {
                        _defaultResults();
                        RevokeQueryTimer();
                    }
                    else
                    {
                        RestartQueryTimer();
                    }
                }
            }
        }

        Object _queryTimer;
        void RevokeQueryTimer()
        {
            if (_queryTimer != null)
            {
                SnooStreamViewModel.SystemServices.StopTimer(_queryTimer);
                _queryTimer = null;
            }
        }

        void RestartQueryTimer()
        {
            // Start or reset a pending query
            if (_queryTimer == null)
            {
                if (_secondsBeforeSearch > 0)
                    _queryTimer = SnooStreamViewModel.SystemServices.StartTimer(queryTimer_Tick, new TimeSpan(0, 0, _secondsBeforeSearch), true);
                else
                    queryTimer_Tick(null, null);
            }
            else
            {
                SnooStreamViewModel.SystemServices.StopTimer(_queryTimer);
                SnooStreamViewModel.SystemServices.RestartTimer(_queryTimer);
            }
        }

        void queryTimer_Tick(object sender, object timer)
        {
            // Stop the timer so it doesn't fire again unless rescheduled
            RevokeQueryTimer();

            if (!(_searchString != null && _searchString.Contains(_alwaysSearchIfContains)))
            {
                _startSearch(_searchString);
            }
        }
    }
}
