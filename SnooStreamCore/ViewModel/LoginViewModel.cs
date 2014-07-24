using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
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
        public LoginViewModel()
        {
            StoredCredentials = new ObservableCollection<UserCredential>();
            LoadStoredCredentials();
        }

        private static string UnselectedUsername = "<None Selected>";

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
				Debug.WriteLine(ex.ToString());
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
                UsingStoredCredential = UnselectedUsername != _selectedCredential.Username;
                if (UsingStoredCredential)
                {
                    Username = SelectedCredential.Username;
                    IsRememberLogin = true;
                    IsDefaultLogin = SelectedCredential.IsDefault;
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

        public void Logout()
        {
            SnooStreamViewModel.RedditUserState.IsGold = false;
            SnooStreamViewModel.RedditUserState.LoginCookie = "";
            SnooStreamViewModel.RedditUserState.ModHash = "";
            SnooStreamViewModel.RedditUserState.NeedsCaptcha = false;
            SnooStreamViewModel.RedditUserState.Username = string.Empty;
        }

        public bool HasStoredLogins { get { return StoredCredentials.Count > 0; } }

        public async void Login()
        {
            if (SnooStreamViewModel.SystemServices.IsHighPriorityNetworkOk)
            {
                Working = true;
                try
                {
                    if (UsingStoredCredential)
                    {
                        // TODO: Do login-y things with the currently selected credential
                    }
                    else
                    {
                        var loggedInUser = await SnooStreamViewModel.RedditService.Login(Username, Password);
                        if (loggedInUser == null)
                        {
                            HasErrors = true;
                            Working = false;
                        }
                        else
                        {
                            HasErrors = false;
                            Working = false;
                            if (IsRememberLogin)
                            {
                                var newCredentials = new UserCredential
                                {
                                    IsDefault = IsDefaultLogin,
                                    LoginCookie = loggedInUser.LoginCookie,
                                    Username = loggedInUser.Username,
                                    Me = new Thing { Kind = "t2", Data = loggedInUser.Me }
                                };
                                await SnooStreamViewModel.UserCredentialService.AddStoredCredential(newCredentials, Password);
                                //reload credentials
                                StoredCredentials.Clear();
                                LoadStoredCredentials();
                                SnooStreamViewModel.RedditUserState.Username = loggedInUser.Username;
                                SnooStreamViewModel.RedditUserState.IsGold = loggedInUser.Me.IsGold;
                                SnooStreamViewModel.RedditUserState.LoginCookie = loggedInUser.LoginCookie;
                                SnooStreamViewModel.RedditUserState.ModHash = loggedInUser.Me.ModHash;
                                SnooStreamViewModel.RedditUserState.NeedsCaptcha = false;
                                MessengerInstance.Send<UserLoggedInMessage>(new UserLoggedInMessage { IsDefault = IsDefaultLogin });
                            }
                        }
                    }
                }
                catch (WebException ex)
                {
                    SnooStreamViewModel.SystemServices.ShowMessage("error", "Login functionality not available in offline mode");
                }
                catch (Exception ex)
                {
                    SnooStreamViewModel.SystemServices.ShowMessage("error", ex.ToString());
                }
            }
            else
            {
                SnooStreamViewModel.SystemServices.ShowMessage("error", "Login functionality not available in offline mode");
            }
        }

        public RelayCommand DoLogout { get { return new RelayCommand(Logout); } }
        public RelayCommand DoLogin { get { return new RelayCommand(Login); } }
        
    }
}
