using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SnooStream.ViewModel.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class CreateMessageViewModel : ViewModelBase
    {
        public string LoggedInUser
        {
            get
            {
                return SnooStreamViewModel.RedditService.CurrentUserName;
            }
        }
        public CreateMessageViewModel ()
	    {
            UsernameSearch = new UsernameSearch();
	    }
        public UsernameSearch UsernameSearch { get; set; }
        private string _topic;
        private string _contents;
        private bool _isReply;

        public string Username
        {
            get
            {
                return UsernameSearch.SearchString;
            }
            set
            {
                UsernameSearch.SearchString = value;
                RaisePropertyChanged("Username");
                RaisePropertyChanged("IsValid");
            }
        }
        public string Topic
        {
            get
            {
                return _topic;
            }
            set
            {
                _topic = value;
                RaisePropertyChanged("Topic");
                RaisePropertyChanged("IsValid");
            }
        }
        public string Contents
        {
            get
            {
                return _contents;
            }
            set
            {
                _contents = value;
                RaisePropertyChanged("Contents");
                RaisePropertyChanged("IsValid");
            }
        }
        public bool IsReply
        {
            get
            {
                return _isReply;
            }
            set
            {
                _isReply = value;
                RaisePropertyChanged("IsReply");
                RaisePropertyChanged("IsValid");
            }
        }
        public bool IsValid
        {
            get
            {
                return !String.IsNullOrWhiteSpace(LoggedInUser) &&
                    !String.IsNullOrWhiteSpace(Username) &&
                    !String.IsNullOrWhiteSpace(Topic) &&
                    !String.IsNullOrWhiteSpace(Contents);

            }
        }
        public RelayCommand Send
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    await SnooStreamViewModel.NotificationService.Report("sending message", async () =>
                    {
                        try
                        {
                            await SnooStreamViewModel.RedditService.AddMessage(Username, Topic, Contents);
                            SnooStreamViewModel.NavigationService.GoBack();
                        }
                        catch
                        {

                        }
                    });
                });
            }
        }
    }
}
