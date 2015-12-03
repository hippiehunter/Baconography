using GalaSoft.MvvmLight;
using SnooSharp;
using SnooStream.ViewModel.Messages;
using SnooStreamBackground;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{

    public interface INavMenuContext
    {
        string Username { get; }
        bool IsMod { get; }
        bool IsLeftSideNav { get; }
        bool HasUnreadMessages { get; }

        ObservableCollection<Subreddit> MRUSubreddits { get; }
    }

    public class NavMenu : ObservableObject
    {
        INavMenuContext _context;
        public NavMenu(INavMenuContext context, LoginViewModel loginViewModel, SelfViewModel selfViewModel, 
            ActivitiesViewModel activitiesViewModel, SettingsViewModel settingsViewModel, SearchViewModel searchViewModel, 
            SubredditRiverViewModel subredditRiverViewModel)
        {
            _context = context;
            GalaSoft.MvvmLight.Messaging.Messenger.Default.Register<SettingsChangedMessage>(this, settingsChanged);
            GalaSoft.MvvmLight.Messaging.Messenger.Default.Register<UserLoggedInMessage>(this, userLoggedIn);
            GalaSoft.MvvmLight.Messaging.Messenger.Default.Register<SubredditSelectedMessage>(this, subredditSelected);
            GalaSoft.MvvmLight.Messaging.Messenger.Default.Register<UnreadMessageCountChangedMessage>(this, messageCountChanged);

            bool isLoggedIn = !string.IsNullOrWhiteSpace(_context.Username);
            bool isMod = _context.IsMod;
            bool hasMessages = _context.HasUnreadMessages;
            Login = new NavMenuItem { Label = "login", Symbol = '\uE13D', VisibleSymbol = true, VM = loginViewModel };
            Self = new NavMenuItem { Label = _context.Username, Symbol = '\uE13D', VisibleSymbol = true, VM = selfViewModel };
            Activity = new NavMenuItem { Label = "activity", Symbol = hasMessages ? '\uE135' : '\uE119', VisibleSymbol = true, VM = activitiesViewModel };
            Settings = new NavMenuItem { Label = "settings", Symbol = '\uE115', VisibleSymbol = true, VM = settingsViewModel };
            Search = new NavMenuItem { Label = "search", Symbol = '\uE11A', VisibleSymbol = true, VM = searchViewModel };
            Subreddits = new NavMenuItem { Label = "subreddits", Symbol = '\uE13D', VisibleSymbol = true, VM = subredditRiverViewModel };

            if (isLoggedIn)
            {
                Items = new ObservableCollection<NavMenuItem>
                {
                    Activity,
                    Self,
                    Search,
                    Settings
                };
            }
            else
            {
                Items = new ObservableCollection<NavMenuItem>
                {
                    Login,
                    Search,
                    Settings
                };
            }

            Items.Add(Subreddits);
            MRUSubreddits = _context.MRUSubreddits;
        }

        private void settingsChanged(SettingsChangedMessage obj)
        {
            LeftSide = _context.IsLeftSideNav;
            RaisePropertyChanged("LeftSide");
        }

        private void messageCountChanged(UnreadMessageCountChangedMessage obj)
        {
            Activity.Symbol = _context.HasUnreadMessages ? '\uE135' : '\uE119';
        }

        private void subredditSelected(SubredditSelectedMessage obj)
        {
            if (MRUSubreddits.Contains(obj.Subreddit))
            {
                MRUSubreddits.Remove(obj.Subreddit);
                MRUSubreddits.Add(obj.Subreddit);
            }
            else
            {
                MRUSubreddits.RemoveAt(0);
                MRUSubreddits.Add(obj.Subreddit);
            }
        }

        private void userLoggedIn(UserLoggedInMessage obj)
        {
            if (obj.NewAccount != null && Items.Contains(Login))
            {
                var loginIndex = Items.IndexOf(Login);
                Items[loginIndex] = Self;
                Items.Insert(loginIndex, Activity);
            }
            else if (obj.NewAccount == null && Items.Contains(Self))
            {
                var selfIndex = Items.IndexOf(Self);
                Items[selfIndex] = Login;
                Items.Remove(Activity);
            }
        }

        public ObservableCollection<NavMenuItem> Items { get; set; }
        private NavMenuItem Login { get; set; }
        private NavMenuItem Self { get; set; }
        private NavMenuItem Activity { get; set; }
        private NavMenuItem Settings { get; set; }
        public NavMenuItem Subreddits { get; set; }
        private NavMenuItem Search { get; set; }
        public ObservableCollection<Subreddit> MRUSubreddits { get; set; }
        public bool LeftSide { get; set; }
    }
    public class NavMenuItem
    {
        public string Label { get; set; }
        public object VM { get; set; }
        public char Symbol { get; set; }
        public bool VisibleSymbol { get; set; }
    }

    class NavMenuContext : INavMenuContext
    {
        public ActivityManager ActivityManager { get; set; }
        public Reddit Reddit { get; set; }
        public bool HasUnreadMessages
        {
            get
            {
                return false;
            }
        }

        public bool IsLeftSideNav
        {
            get
            {
                return true;
            }
        }

        public bool IsMod
        {
            get
            {
                return Reddit.CurrentUserIsMod;
            }
        }

        public ObservableCollection<Subreddit> MRUSubreddits
        {
            get
            {
                return new ObservableCollection<Subreddit>();
            }
        }

        public string Username
        {
            get
            {
                return Reddit.CurrentUserName;
            }
        }
    }
}
