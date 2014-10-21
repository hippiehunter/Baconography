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
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class LoginViewModel : ViewModelBase
    {
		ILogger _logger = LogManagerFactory.DefaultLogManager.GetLogger<LoginViewModel>();
        public LoginViewModel()
        {
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
                        IsDefaultLogin = SelectedCredential.IsDefault;
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

        string _password;
        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                _password = value;
                RaisePropertyChanged("Password");
            }
        }

        private bool _hasErrors = false;
        public bool HasErrors
        {
            get
            {
                return _hasErrors;
            }
            set
            {
                _hasErrors = value;
                RaisePropertyChanged("HasErrors");
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

        private bool _isDefaultLogin;
        public bool IsDefaultLogin
        {
            get
            {
                return _isDefaultLogin;
            }
            set
            {
                _isDefaultLogin = value;
                RaisePropertyChanged("IsDefaultLogin");
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

		private string _errorText;
		public string ErrorText
		{
			get
			{
				return _errorText;
			}
			set
			{
				_errorText = value;
				RaisePropertyChanged("ErrorText");
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
			HasErrors = true;
			ErrorText = errorText;
		}

		public async void FinishOAuth(string code)
		{
			try
			{
				Working = true;
				var oAuth = await SnooStreamViewModel.RedditService.RequestGrantCode(code);
				SnooStreamViewModel.RedditUserState.OAuth = oAuth;
				var currentAccount = await SnooStreamViewModel.RedditService.GetIdentity();
				SnooStreamViewModel.RedditUserState.IsGold = currentAccount.IsGold;
				SnooStreamViewModel.RedditUserState.LoginCookie = "";
				SnooStreamViewModel.RedditUserState.ModHash = "";
				SnooStreamViewModel.RedditUserState.NeedsCaptcha = false;
				Username = SnooStreamViewModel.RedditUserState.Username = currentAccount.Name;
				SnooStreamViewModel.RedditUserState.IsDefault = IsDefaultLogin;
				GalaSoft.MvvmLight.Messaging.Messenger.Default.Send<UserLoggedInMessage>(new UserLoggedInMessage { IsDefault = IsDefaultLogin });
			}
			finally
			{
				Working = false;
			}
		}

        public RelayCommand DoLogout { get { return new RelayCommand(Logout); } }
        public RelayCommand DoLogin { get { return new RelayCommand(Login); } }


		
	}
}
