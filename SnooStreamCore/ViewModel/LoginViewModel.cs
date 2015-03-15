using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MetroLog;
using SnooSharp;
using SnooStream.Messages;
using SnooStream.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class LoginViewModel : ViewModelBase, ICancellableViewModel
    {
        CancellationToken _token;
		ILogger _logger = LogManagerFactory.DefaultLogManager.GetLogger<LoginViewModel>();
        public LoginViewModel()
        {
            IsAutoLogin = true;
            StoredCredentials = new ObservableCollection<UserCredential>();
            LoadStoredCredentials();
        }

        private static string UnselectedUsername = "<None Selected>";

		public bool IsLoggedIn
		{
			get
			{
				return !string.IsNullOrWhiteSpace(SnooStreamViewModel.RedditUserState.Username);
			}
		}
        private async void LoadStoredCredentials()
        {
			try
			{
				var storedCredentials = await SnooStreamViewModel.UserCredentialService.StoredCredentials();
				foreach (var credential in storedCredentials)
				{
					StoredCredentials.Add(credential);
				}

				if (StoredCredentials.Count > 0)
				{
					_selectedCredential = StoredCredentials.First();
					StoredCredentials.Add(new UserCredential { Username = UnselectedUsername, IsDefault = false });
				}
				RaisePropertyChanged("StoredCredentials");
				RaisePropertyChanged("HasStoredLogins");
				RaisePropertyChanged("SelectedCredential");
			}
			catch(Exception ex)
			{
				_logger.Error("failed loading stored credentials", ex);
			}
        }

        public ObservableCollection<UserCredential> StoredCredentials
        {
            get;
            private set;
        }

        UserCredential _selectedCredential;
        public UserCredential SelectedCredential
        {
            get
            {
                return _selectedCredential;
            }
            set
            {
                _selectedCredential = value;
                RaisePropertyChanged("SelectedCredential");
                RaisePropertyChanged("SelectedUsername");
                if (_selectedCredential != null)
                {
                    UsingStoredCredential = UnselectedUsername != _selectedCredential.Username;
                    if (UsingStoredCredential)
                    {
                        Username = SelectedCredential.Username;
                        IsRememberLogin = true;
                        IsAutoLogin = SelectedCredential.IsDefault;
                    }
                }
            }
        }

        bool _usingStoredCredential;
        public bool UsingStoredCredential
        {
            get
            {
                return _usingStoredCredential;
            }
            set
            {
                _usingStoredCredential = value;
                RaisePropertyChanged("UsingStoredCredential");
            }
        }

        public string SelectedUsername
        {
            get
            {
                return SelectedCredential != null ? SelectedCredential.Username : "None";
            }
        }

        string _username;
        public string Username
        {
            get
            {
                return _username;
            }
            set
            {
                _username = value;
                RaisePropertyChanged("Username");
				RaisePropertyChanged("IsLoggedIn");
            }
        }

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

        public void Logout()
        {
            SnooStreamViewModel.RedditUserState.IsGold = false;
            SnooStreamViewModel.RedditUserState.LoginCookie = "";
            SnooStreamViewModel.RedditUserState.ModHash = "";
            SnooStreamViewModel.RedditUserState.NeedsCaptcha = false;
            SnooStreamViewModel.RedditUserState.Username = string.Empty;
        }

        public bool HasStoredLogins { get { return StoredCredentials.Count > 0; } }

        public void Login()
        {
            if (SnooStreamViewModel.SystemServices.IsHighPriorityNetworkOk)
            {
                Working = true;
				SnooStreamViewModel.SystemServices.ShowOAuthBroker();
            }
            else
            {
                SnooStreamViewModel.SystemServices.ShowMessage("error", "Login functionality not available in offline mode");
            }
        }

		public void FailOAuth(string errorText, string responseText)
		{
			ResultText = errorText;
		}

		public async void FinishOAuth(string code)
		{
            try
            {
                Finished = false;
                SnooStreamViewModel.NavigationService.NavigateToOAuthLanding(this);
                Working = true;
                var oAuth = await SnooStreamViewModel.RedditService.RequestGrantCode(code, _token);
                SnooStreamViewModel.RedditUserState.OAuth = oAuth;
                var currentAccount = await SnooStreamViewModel.RedditService.GetIdentity();
                ResultText = "Successfully logged in as " + currentAccount.Name;
                SnooStreamViewModel.RedditUserState.IsGold = currentAccount.IsGold;
                SnooStreamViewModel.RedditUserState.LoginCookie = "";
                SnooStreamViewModel.RedditUserState.ModHash = "";
                SnooStreamViewModel.RedditUserState.NeedsCaptcha = false;
                Username = SnooStreamViewModel.RedditUserState.Username = currentAccount.Name;
                SnooStreamViewModel.RedditUserState.IsDefault = IsAutoLogin;
                GalaSoft.MvvmLight.Messaging.Messenger.Default.Send<UserLoggedInMessage>(new UserLoggedInMessage { IsDefault = IsAutoLogin, NewAccount = currentAccount });
                Success = true;
            }
            catch(Exception ex)
            {
                Success = false;
                ResultText = ex.Message;
            }
			finally
			{
				Working = false;
			}
            if(Success)
            {
                Finished = true;
            }
		}

        public bool BindToken(CancellationToken token)
        {
            _token = token;
            return true;
        }

        public RelayCommand DoLogout { get { return new RelayCommand(Logout); } }
        public RelayCommand DoLogin { get { return new RelayCommand(Login); } }
        public RelayCommand ContinueOAuthCommand { get { return new RelayCommand(() => SnooStreamViewModel.NavigationService.GoBack()); } }
        public RelayCommand RetryOAuthCommand { get { return new RelayCommand(Login); } }
        public RelayCommand CancelOAuthCommand { get { return new RelayCommand(() => SnooStreamViewModel.NavigationService.GoBack()); } }
        
		
	}
}
