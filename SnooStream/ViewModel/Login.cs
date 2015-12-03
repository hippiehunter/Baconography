using GalaSoft.MvvmLight;
using Newtonsoft.Json;
using SnooSharp;
using SnooStream.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;

namespace SnooStream.ViewModel
{
    public class LoginViewModel : ObservableObject
    {
        private ILoginContext _context;

        public LoginViewModel(ILoginContext context)
        {
            _context = context;
        }

        public bool IsLoggedIn { get; }
        public bool UsingStoredCredential { get; }

        private bool _working = false;
        public bool Working
        {
            get
            {
                return _working;
            }
            set
            {
                _working = value;
                RaisePropertyChanged("Working");
            }
        }

        private bool _success = true;
        public bool Success
        {
            get
            {
                return _success;
            }
            set
            {
                _success = value;
                RaisePropertyChanged("Success");
            }
        }

        private bool _finished = true;
        public bool Finished
        {
            get
            {
                return _finished;
            }
            set
            {
                _finished = value;
                RaisePropertyChanged("Finished");
            }
        }

        private bool _isAutoLogin;
        public bool IsAutoLogin
        {
            get
            {
                return _isAutoLogin;
            }
            set
            {
                _isAutoLogin = value;
                RaisePropertyChanged("IsAutoLogin");
            }
        }

        private bool _isMod;
        public bool IsMod
        {
            get
            {
                return _isMod;
            }
            set
            {
                _isMod = value;
                RaisePropertyChanged("IsMod");
            }
        }

        private bool _isRememberLogin;
        public bool IsRememberLogin
        {
            get
            {
                return _isRememberLogin;
            }
            set
            {
                _isRememberLogin = value;
                RaisePropertyChanged("IsRememberLogin");
            }
        }

        private string _resultText;
        public string ResultText
        {
            get
            {
                return _resultText;
            }
            set
            {
                _resultText = value;
                RaisePropertyChanged("ResultText");
            }
        }

        public ObservableCollection<UserState> StoredCredentials
        {
            get;
            private set;
        }

        public void Login()
        {
            _context.ShowOAuthBroker();
        }

        public async Task FinishLoginAsync(string code)
        {
            _continue = new TaskCompletionSource<bool>();
            Finished = false;
            Working = true;
            var oAuth = await _context.RequestGrantCode(code, _cancelToken);
            var user = await _context.SetActiveLogin(oAuth);
            ResultText = "Successfully logged in as " + user.Username;
            IsMod = user.Me.IsMod;

            Success = true;
            Finished = true;
            //wait for the user to press continue so we can gather the default/store info
            await _continue.Task;
            if (IsRememberLogin)
            {
                var newCredential = new UserState { Username = user.Username, IsDefault = IsAutoLogin, OAuth = oAuth };
                await _context.AddStoredCredential(newCredential);
                StoredCredentials.Add(newCredential);
            }
        }

        public void ContinueOAuthCommand()
        {
            if (_continue != null)
                _continue.TrySetResult(true);
        }
        TaskCompletionSource<bool> _continue = new TaskCompletionSource<bool>();
        CancellationToken _cancelToken = new CancellationToken(false);
    }

    public interface ILoginContext
    {
        void ShowOAuthBroker();
        Task<RedditOAuth> RequestGrantCode(string code, CancellationToken cancelToken);
        Task<IEnumerable<UserState>> StoredCredentials();
        Task AddStoredCredential(UserState newCredential);
        Task RemoveStoredCredential(string username);
        Task<User> SetActiveLogin(RedditOAuth credential);
    }

    class LoginContext : ILoginContext
    {
        public RoamingState RoamingState { get; set; }
        public Reddit Reddit { get; set; }
        public Task AddStoredCredential(UserState newCredential)
        {
            var credentials = RoamingState.UserCredentials;
            credentials.Add(newCredential);
            RoamingState.UserCredentials = credentials;
            return Task.CompletedTask;
        }

        public Task RemoveStoredCredential(string username)
        {
            var credentials = RoamingState.UserCredentials;
            credentials.RemoveAll(state => state.Username == username);
            RoamingState.UserCredentials = credentials;
            return Task.CompletedTask;
        }

        public Task<RedditOAuth> RequestGrantCode(string code, CancellationToken cancelToken)
        {
            return Reddit.RequestGrantCode(code, cancelToken);
        }

        public async Task<User> SetActiveLogin(RedditOAuth credential)
        {
            var gottenAccount = await Reddit.ChangeIdentity(credential);
            return new User { Authenticated = true, Me = gottenAccount, Username = gottenAccount.Name };
        }

        public void ShowOAuthBroker()
        {
            String RedditURL = string.Format("https://ssl.reddit.com/api/v1/authorize?client_id={0}&response_type={1}&state={2}&redirect_uri={3}&duration={4}&scope={5}",
                "3m9rQtBinOg_rA", "code", "something", "http://www.google.com", "permanent", "modposts,identity,edit,flair,history,modconfig,modflair,modlog,modposts,modwiki,mysubreddits,privatemessages,read,report,save,submit,subscribe,vote,wikiedit,wikiread");

            System.Uri StartUri = new Uri(RedditURL);
            System.Uri EndUri = new Uri("http://www.google.com");
			WebAuthenticationBroker.AuthenticateAndContinue(StartUri, EndUri, null, WebAuthenticationOptions.None);
        }

        public Task<IEnumerable<UserState>> StoredCredentials()
        {
            return Task.FromResult((IEnumerable<UserState>)RoamingState.UserCredentials);
        }
    }
}
