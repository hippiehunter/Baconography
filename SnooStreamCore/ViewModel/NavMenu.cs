using GalaSoft.MvvmLight;
using SnooStream.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class NavMenu : ViewModelBase
    {
        SnooStreamViewModel _snooStream;
        public NavMenu(IEnumerable<LinkRiverViewModel> mruList, SnooStreamViewModel snooStream)
        {
            _snooStream = snooStream;
            GalaSoft.MvvmLight.Messaging.Messenger.Default.Register<SettingsChangedMessage>(this, settingsChanged);
            GalaSoft.MvvmLight.Messaging.Messenger.Default.Register<UserLoggedInMessage>(this, userLoggedIn);
            GalaSoft.MvvmLight.Messaging.Messenger.Default.Register<SubredditSelectedMessage>(this, subredditSelected);
            GalaSoft.MvvmLight.Messaging.Messenger.Default.Register<UnreadMessageCountChangedMessage>(this, messageCountChanged);

            bool isLoggedIn = !string.IsNullOrWhiteSpace(SnooStreamViewModel.RedditUserState.Username);
            bool isMod = SnooStreamViewModel.RedditUserState.IsMod;
            bool hasMessages = snooStream.SelfStream.HasUnviewed;
            Login = new NavMenuItem { Label = "login", Symbol = '\uE13D', VisibleSymbol = true, VM = snooStream.Login };
            Self = new NavMenuItem { Label = SnooStreamViewModel.RedditUserState.Username, Symbol = '\uE13D', VisibleSymbol = true, VM = snooStream.SelfUser };
            Activity = new NavMenuItem { Label = "activity", Symbol = hasMessages ? '\uE135' : '\uE119' , VisibleSymbol = true, VM = snooStream.SelfStream };
            Moderation = new NavMenuItem { Label = "moderation", Symbol = '\uE178', VisibleSymbol = true, VM = snooStream.SelfUser };
            Settings = new NavMenuItem { Label = "settings", Symbol = '\uE115', VisibleSymbol = true, VM = snooStream.SettingsHub };
            Search = new NavMenuItem { Label = "search", Symbol = '\uE11A', VisibleSymbol = true, VM = snooStream.SelfUser };
            Subreddits = new NavMenuItem { Label = "subreddits", Symbol = '\uE13D', VisibleSymbol = true, VM = snooStream.SubredditRiver };

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

            if (isMod)
                Items.Add(Moderation);

            Items.Add(Subreddits);
            MRUSubreddits = new ObservableCollection<LinkRiverViewModel>(mruList);
        }

        private void settingsChanged(SettingsChangedMessage obj)
        {
            LeftSide = SnooStreamViewModel.Settings.LeftSideNav;
            RaisePropertyChanged("LeftSide");
        }

        private void messageCountChanged(UnreadMessageCountChangedMessage obj)
        {
            Activity.Symbol = _snooStream.SelfStream.HasUnviewed ? '\uE135' : '\uE119';
        }

        private void subredditSelected(SubredditSelectedMessage obj)
        {
            if (MRUSubreddits.Contains(obj.ViewModel))
            {
                MRUSubreddits.Remove(obj.ViewModel);
                MRUSubreddits.Add(obj.ViewModel);
            }
            else
            {
                MRUSubreddits.RemoveAt(0);
                MRUSubreddits.Add(obj.ViewModel);
            }
        }

        private void userLoggedIn(UserLoggedInMessage obj)
        {
            if (obj.NewAccount != null && Items.Contains(Login))
            {
                var loginIndex = Items.IndexOf(Login);
                Items[loginIndex] = Self;
                Items.Insert(loginIndex, Activity);
                if (SnooStreamViewModel.RedditUserState.IsMod)
                    Items.Insert(loginIndex + 2, Moderation);
            }
            else if (obj.NewAccount == null && Items.Contains(Self))
            {
                var selfIndex = Items.IndexOf(Self);
                Items[selfIndex] = Login;
                Items.Remove(Moderation);
                Items.Remove(Activity);
            }
        }

        public ObservableCollection<NavMenuItem> Items { get; set; }
        private NavMenuItem Login { get; set; }
        private NavMenuItem Self { get; set; }
        private NavMenuItem Activity { get; set; }
        private NavMenuItem Settings { get; set; }
        public NavMenuItem Subreddits { get; set; }
        private NavMenuItem Moderation { get; set; }
        private NavMenuItem Search { get; set; }
        public ObservableCollection<LinkRiverViewModel> MRUSubreddits { get; set; }
        public bool LeftSide { get; set; }
    }
    public class NavMenuItem
    {
        public string Label { get; set; }
        public object VM { get; set; }
        public char Symbol { get; set; }
        public bool VisibleSymbol { get; set; }
    }
}
