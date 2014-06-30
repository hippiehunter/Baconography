using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SnooSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class PostViewModel : ViewModelBase
    {
        public PostViewModel() 
        {
            Kind = "link";
            _refreshUser = new RelayCommand(RefreshUserImpl);
            _submit = new RelayCommand(SubmitImpl);
        }

        public PostViewModel(Link link) : this()
        {
            Kind = link.IsSelf ? "self" : "link";
            Subreddit = link.Subreddit;
            Title = link.Title;
            Text = link.Selftext;
            _name = link.Name;
            Editing = true;
        }
        private bool _editing = false;
        public bool Editing
        {
            get
            {
                return _editing;
            }
            set
            {
                _editing = value;
                RaisePropertyChanged("Editing");
            }
        }

        private string _name;

        private string _kind;
        public string Kind
        {
            get
            {
                return _kind;
            }
            set
            {
                _kind = value;
                RaisePropertyChanged("Kind");
            }
        }

        private string _subreddit;
        public string Subreddit
        {
            get
            {
                return _subreddit;
            }
            set
            {
                _subreddit = value;
                RaisePropertyChanged("Subreddit");
            }
        }

        private string _title;
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;
                RaisePropertyChanged("Title");
            }
        }

        private string _text;
        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
                RaisePropertyChanged("Text");
            }
        }

        private string _url;
        public string Url
        {
            get
            {
                return _url;
            }
            set
            {
                _url = value;
                RaisePropertyChanged("Url");
            }
        }

        private string _postingAs;
        public string PostingAs
        {
            get
            {
                return _postingAs;
            }
            set
            {
                _postingAs = value;
                RaisePropertyChanged("PostingAs");
            }
        }

        private bool _isLoggedIn;
        public bool IsLoggedIn
        {
            get
            {
                return _isLoggedIn;
            }
            set
            {
                _isLoggedIn = value;
                RaisePropertyChanged("IsLoggedIn");
                RaisePropertyChanged("CanSend");
            }
        }

        public bool CanSend
        {
            get
            {
                return IsLoggedIn
                    && (!String.IsNullOrWhiteSpace(Text) || !String.IsNullOrWhiteSpace(Url))
                    && !String.IsNullOrWhiteSpace(Title)
                    && !String.IsNullOrWhiteSpace(Subreddit);
            }
        }

        public RelayCommand Submit { get { return _submit; } }
        private RelayCommand _submit;
        private void SubmitImpl()
        {
            if (Url == null)
                Url = "";
            if (Text == null)
                Text = "";


            SnooStreamViewModel.NotificationService.Report("posting to reddit", async () =>
            {
                if (!Editing)
                {
                    await SnooStreamViewModel.RedditService.AddPost(Kind, Url, Text, Subreddit, Title);
                }
                else
                {
                    await SnooStreamViewModel.RedditService.EditPost(Text, _name);
                }

                Url = "";
                Text = "";
                Subreddit = "";
                Title = "";

                SnooStreamViewModel.NavigationService.GoBack();
            });
        }

        public RelayCommand RefreshUser { get { return _refreshUser; } }
        private RelayCommand _refreshUser;
        private void RefreshUserImpl()
        {
            if (string.IsNullOrWhiteSpace(SnooStreamViewModel.RedditService.CurrentUserName))
            {
                IsLoggedIn = false;
                PostingAs = string.Empty;
            }
            else
            {
                PostingAs = SnooStreamViewModel.RedditService.CurrentUserName;
                IsLoggedIn = true;
            }
        }
    }
}
