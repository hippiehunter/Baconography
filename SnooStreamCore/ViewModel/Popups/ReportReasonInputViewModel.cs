using SnooSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel.Popups
{
    public class ReportReasonInputViewModel : InputViewModel
    {
        ThingData _thingData;
        public ReportReasonInputViewModel(ThingData thingData) :
            base("reason for the report", "", Enumerable.Empty<string>(), null)
        {
            _thingData = thingData;
            Dismissed = new GalaSoft.MvvmLight.Command.RelayCommand<string>(OnDismissed);
        }

        public void OnDismissed(string inputValue)
        {
            SnooStreamViewModel.RedditService.AddReportOnThing(_thingData.Name, inputValue);
        }
    }
}
