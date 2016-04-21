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
using Windows.Foundation;
using Windows.Security.Authentication.Web;
using Windows.UI.Xaml.Controls;

namespace SnooStream.ViewModel
{
    public class LoginViewModel : SnooObservableObject
    {
        public ILoginContext Context;

        public LoginViewModel(ILoginContext context)
        {
            Context = context;
            StoredCredentials = new ObservableCollection<UserState>();
            Context.StoredCredentials().ContinueWith(async tsk =>
            {
                var credentials = await tsk;
                if(credentials != null)
                    foreach (var credential in credentials)
                        StoredCredentials.Add(credential);
            });
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

        public bool HasStoredLogins
        {
            get
            {
                return StoredCredentials != null && StoredCredentials.Count > 0;
            }
        }

        public UserState SelectedCredential
        {
            get
            {
                var activeLogin = Context.ActiveLogin;
                return StoredCredentials?.FirstOrDefault(credential => credential.Username == activeLogin.Username) ?? activeLogin;
            }
            set
            {
                Context.SetActiveLogin(value?.OAuth);
            }
        }

        public void Login()
        {
            Context.ShowOAuthBroker();
        }

        public void Delete()
        {
            throw new NotImplementedException();
        }

        public void Logout()
        {
            Context.SetActiveLogin(null);
        }

        public async Task FinishLoginAsync(string code)
        {
            _continue = new TaskCompletionSource<bool>();
            Finished = false;
            Working = true;
            var oAuth = await Context.RequestGrantCode(code, _cancelToken.Token);
            var user = await Context.SetActiveLogin(oAuth);
            ResultText = "Successfully logged in as " + user.Username;
            IsMod = user.Me.IsMod;

            Success = true;
            Finished = true;
            RaisePropertyChanged("Success");
            RaisePropertyChanged("Finished");
            //wait for the user to press continue so we can gather the default/store info
            await _continue.Task;
            if (IsRememberLogin)
            {
                var newCredential = new UserState { Username = user.Username, IsDefault = IsAutoLogin, OAuth = oAuth };
                await Context.AddStoredCredential(newCredential);
                StoredCredentials.Add(newCredential);
            }
        }

        public void FailLogin(string resultText)
        {
            ResultText = resultText;
            RaisePropertyChanged("ResultText");
            RaisePropertyChanged("Success");
            RaisePropertyChanged("Finished");
        }

        public void Cancel()
        {
            _cancelToken.Cancel();
        }

        public void ContinueOAuthCommand()
        {
            if (_continue != null)
                _continue.TrySetResult(true);
        }
        TaskCompletionSource<bool> _continue = new TaskCompletionSource<bool>();
        CancellationTokenSource _cancelToken = new CancellationTokenSource();
    }

    public interface ILoginContext
    {
        void ShowOAuthBroker();
        Task HandleOAuth(WebAuthenticationResult result, ContentDialog dialog = null);
        Task<RedditOAuth> RequestGrantCode(string code, CancellationToken cancelToken);
        Task<IEnumerable<UserState>> StoredCredentials();
        Task AddStoredCredential(UserState newCredential);
        Task RemoveStoredCredential(string username);
        Task<User> SetActiveLogin(RedditOAuth credential);
        UserState ActiveLogin { get; }
        //Client is required to keep the lifetime of the passed in listener
        void AddUserChangeWeakListener(Action<RedditOAuth> listener);
    }

    class LoginContext : ILoginContext
    {
        public RoamingState RoamingState { get; set; }
        public Reddit Reddit { get; set; }
        public NavigationContext Navigation { get; set; }
        public UserState ActiveLogin
        {
            get
            {
                return new UserState { OAuth = Reddit.CurrentOAuth, Username = Reddit.CurrentUserName };
            }
        }

        WeakListener<RedditOAuth> _userChangeListerner = new WeakListener<RedditOAuth>();
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
            _userChangeListerner.TriggerListeners(credential);
            return new User { Authenticated = true, Me = gottenAccount, Username = gottenAccount.Name };
        }

        private void ContinueOAuth(ContentDialog dialog, ContentDialogButtonClickEventArgs args)
        {
            Navigation.LoginViewModel.ContinueOAuthCommand();
            MainPage.Current.NavContext.HubNav.Back();
        }

        private void RetryOAuth(ContentDialog dialog, ContentDialogButtonClickEventArgs args)
        {
            dialog.Hide();
            ShowOAuthBroker();
        }

        private ContentDialog MakeOAuthDialog()
        {
            var dialog = new ContentDialog()
            {
                Title = "Login",
                MaxWidth = 480, // Required for Mobile!
                ContentTemplate = Navigation.OAuthLandingTemplate,
                Content = Navigation.LoginViewModel
            };


            dialog.PrimaryButtonText = "Continue";
            dialog.SecondaryButtonText = "Cancel";
            dialog.IsSecondaryButtonEnabled = true;

            dialog.SecondaryButtonClick += delegate
            {
                Navigation.LoginViewModel.Cancel();
                MainPage.Current.NavContext.HubNav.Back();
            };

            dialog.IsPrimaryButtonEnabled = false;
            dialog.PrimaryButtonClick += ContinueOAuth;

            var result = dialog.ShowAsync();
            return dialog;
        }

        public async Task HandleOAuth(WebAuthenticationResult result, ContentDialog dialog = null)
        {
            if (dialog == null)
            {
                dialog = MakeOAuthDialog();
            }

            if (result.ResponseStatus == Windows.Security.Authentication.Web.WebAuthenticationStatus.Success)
            {
                dialog.IsPrimaryButtonEnabled = true;
                var resultData = result.ResponseData;
                var decoder = new WwwFormUrlDecoder(new Uri(resultData).Query);
                var code = decoder.GetFirstValueByName("code");
                await MainPage.Current.NavContext.LoginViewModel.FinishLoginAsync(code);
            }
            else
            {
                dialog.PrimaryButtonClick -= ContinueOAuth;
                dialog.PrimaryButtonText = "Retry";
                dialog.IsPrimaryButtonEnabled = true;
                dialog.PrimaryButtonClick += RetryOAuth;
                MainPage.Current.NavContext.LoginViewModel.FailLogin(result.ResponseData);
            }
        }

        public async void ShowOAuthBroker()
        {
            var dialog = MakeOAuthDialog();
            String RedditURL = string.Format("https://ssl.reddit.com/api/v1/authorize?client_id={0}&response_type={1}&state={2}&redirect_uri={3}&duration={4}&scope={5}",
                "3m9rQtBinOg_rA", "code", "something", "http://www.google.com", "permanent", "modposts,identity,edit,flair,history,modconfig,modflair,modlog,modposts,modwiki,mysubreddits,privatemessages,read,report,save,submit,subscribe,vote,wikiedit,wikiread");

            System.Uri StartUri = new Uri(RedditURL);
            System.Uri EndUri = new Uri("http://www.google.com");
            if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
            {
                Windows.Security.Authentication.Web.WebAuthenticationBroker.AuthenticateAndContinue(StartUri, EndUri, null, Windows.Security.Authentication.Web.WebAuthenticationOptions.None);
            }
            else
            {
                var authResult = await Windows.Security.Authentication.Web.WebAuthenticationBroker.AuthenticateAsync(
                    Windows.Security.Authentication.Web.WebAuthenticationOptions.None, StartUri, EndUri);

                await HandleOAuth(authResult, dialog);
            }
        }

        public Task<IEnumerable<UserState>> StoredCredentials()
        {
            return Task.FromResult((IEnumerable<UserState>)RoamingState.UserCredentials);
        }

        public void AddUserChangeWeakListener(Action<RedditOAuth> listener)
        {
            _userChangeListerner.AddListener(listener);
        }
    }
}
