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
			IsRememberLogin = true;
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
				var notLoggedIn = new UserCredential { Username = UnselectedUsername, IsDefault = false };
				
				var storedCredentials = await SnooStreamViewModel.UserCredentialService.StoredCredentials();
				foreach (var credential in storedCredentials)
				{
					StoredCredentials.Add(credential);
				}

				StoredCredentials.Add(notLoggedIn);
				_selectedCredential = StoredCredentials.FirstOrDefault(stored => string.Compare(stored.Username, SnooStreamViewModel.RedditUserState.Username, StringComparison.CurrentCultureIgnoreCase) == 0) ?? notLoggedIn;

				IsAutoLogin = _selectedCredential == notLoggedIn || notLoggedIn.IsDefault;

				RaisePropertyChanged("StoredCredentials");
				RaisePropertyChanged("HasStoredLogins");
				RaisePropertyChanged("SelectedCredential");
			}
			catch (Exception ex)
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
				if (_selectedCredential != value)
				{
					_selectedCredential = value;
					RaisePropertyChanged("SelectedCredential");

					if (_selectedCredential != null)
					{
						if (_selectedCredential.Username == UnselectedUsername)
							Logout();
						else if (string.Compare(_selectedCredential.Username, Username, StringComparison.CurrentCultureIgnoreCase) != 0)
							Login(_selectedCredential.Username);
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

		public string Username
		{
			get
			{
				return SnooStreamViewModel.RedditUserState.Username;
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

		private async void Delete()
		{
			try
			{
				if (SnooStreamViewModel.RedditUserState != null && SnooStreamViewModel.RedditUserState.OAuth != null)
				{
					await SnooStreamViewModel.RedditService.DestroyToken(SnooStreamViewModel.RedditUserState.OAuth.RefreshToken);
					await SnooStreamViewModel.UserCredentialService.RemoveStoredCredential(SnooStreamViewModel.RedditUserState.Username);
					var foundCredential = StoredCredentials.FirstOrDefault(cred => cred.Username == UnselectedUsername);
					if(foundCredential != null)
						StoredCredentials.Remove(foundCredential);

					RaisePropertyChanged("HasStoredLogins");
				}

				Logout();
			}
			catch(Exception ex)
			{
				_logger.Error("failure while deleting authorization", ex);
			}
		}

		public void Logout()
		{
			if (!string.IsNullOrEmpty(SnooStreamViewModel.RedditUserState.Username))
			{

				SnooStreamViewModel.RedditUserState.IsGold = false;
				SnooStreamViewModel.RedditUserState.LoginCookie = "";
				SnooStreamViewModel.RedditUserState.ModHash = "";
				SnooStreamViewModel.RedditUserState.NeedsCaptcha = false;
				SnooStreamViewModel.RedditUserState.Username = string.Empty;

				SnooStreamViewModel.RedditUserState.OAuth = null;
				SnooStreamViewModel.RedditUserState.Username = null;
				SnooStreamViewModel.RedditUserState.IsDefault = false;
				SnooStreamViewModel.RedditUserState.IsGold = false;
				SnooStreamViewModel.RedditUserState.LoginCookie = "";
				SnooStreamViewModel.RedditUserState.ModHash = "";
				SnooStreamViewModel.RedditUserState.NeedsCaptcha = false;
				SnooStreamViewModel.RedditUserState.Username = null;
				RaisePropertyChanged("IsLoggedIn");
				GalaSoft.MvvmLight.Messaging.Messenger.Default.Send<UserLoggedInMessage>(new UserLoggedInMessage { IsDefault = false, NewAccount = null });
			}

			SelectedCredential = StoredCredentials.First(cred => cred.Username == UnselectedUsername);
		}

		public bool HasStoredLogins
		{
			get
			{
				//we dont want to show a list with one item if that one item is the currently logged in user anyway
				if (StoredCredentials.Count > 0)
				{
					if (StoredCredentials.Count > 2)
						return true;
					else if (StoredCredentials.First().Username == Username || StoredCredentials.First().Username == UnselectedUsername)
						return false;
					else
						return true;
				}
				else
					return false;
			}
		}

		public async void Login(string storedUsername)
		{
			if (SnooStreamViewModel.SystemServices.IsHighPriorityNetworkOk)
			{
				Working = true;
				if (string.IsNullOrWhiteSpace(storedUsername) || storedUsername == UnselectedUsername)
					Logout();
				else if (string.Compare(storedUsername, Username, StringComparison.CurrentCultureIgnoreCase) != 0)
				{
					var targetCredential = StoredCredentials.FirstOrDefault((credential) => string.Compare(storedUsername, credential.Username, StringComparison.CurrentCultureIgnoreCase) == 0);
					if (targetCredential != null)
					{
						await SnooStreamViewModel.NotificationService.Report("switching user", async () =>
						{
							SnooStreamViewModel.RedditUserState.OAuth = targetCredential.OAuth;
							SnooStreamViewModel.RedditUserState.Username = null;
							SnooStreamViewModel.RedditUserState.IsDefault = targetCredential.IsDefault;
							var currentAccount = await SnooStreamViewModel.RedditService.GetIdentity();
							SnooStreamViewModel.RedditUserState.IsMod = currentAccount.IsMod;
							SnooStreamViewModel.RedditUserState.IsGold = currentAccount.IsGold;
							SnooStreamViewModel.RedditUserState.LoginCookie = "";
							SnooStreamViewModel.RedditUserState.ModHash = "";
							SnooStreamViewModel.RedditUserState.NeedsCaptcha = false;
							SnooStreamViewModel.RedditUserState.Username = currentAccount.Name;
							GalaSoft.MvvmLight.Messaging.Messenger.Default.Send<UserLoggedInMessage>(new UserLoggedInMessage { IsDefault = IsAutoLogin, NewAccount = currentAccount });
						});
					}
				}
			}
			else
			{
				SnooStreamViewModel.SystemServices.ShowMessage("error", "Login functionality not available in offline mode");
			}
		}

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
				_continue = new TaskCompletionSource<bool>();
				Finished = false;
				SnooStreamViewModel.NavigationService.NavigateToOAuthLanding(this);
				Working = true;
				var oAuth = await SnooStreamViewModel.RedditService.RequestGrantCode(code, _token);
				SnooStreamViewModel.RedditUserState.OAuth = oAuth;
				var currentAccount = await SnooStreamViewModel.RedditService.GetIdentity();
				ResultText = "Successfully logged in as " + currentAccount.Name;
				IsMod = currentAccount.IsMod;
				SnooStreamViewModel.RedditUserState.IsGold = currentAccount.IsGold;
				SnooStreamViewModel.RedditUserState.LoginCookie = "";
				SnooStreamViewModel.RedditUserState.ModHash = "";
				SnooStreamViewModel.RedditUserState.NeedsCaptcha = false;
				SnooStreamViewModel.RedditUserState.Username = currentAccount.Name;
				Success = true;
				Finished = true;
				//wait for the user to press continue so we can gather the default/store info
				await _continue.Task;
				SnooStreamViewModel.RedditUserState.IsDefault = IsAutoLogin;
				if (IsRememberLogin)
				{
                    var newCredential = new UserCredential { Username = Username, IsDefault = IsAutoLogin, Me = new Thing { Data = currentAccount, Kind = "t2" }, OAuth = oAuth };
                    await SnooStreamViewModel.UserCredentialService.AddStoredCredential(newCredential);
                    StoredCredentials.Add(newCredential);

                    //dont fire the login stuff just use the field directly
                    _selectedCredential = newCredential;

                    RaisePropertyChanged("HasStoredLogins");
                    RaisePropertyChanged("SelectedCredential");
				}
				GalaSoft.MvvmLight.Messaging.Messenger.Default.Send<UserLoggedInMessage>(new UserLoggedInMessage { IsDefault = IsAutoLogin, NewAccount = currentAccount });
			}
			catch (Exception ex)
			{
				Success = false;
				ResultText = ex.Message;
			}
			finally
			{
				Working = false;
			}
			if (Success)
			{
				Finished = true;
			}
		}

		public bool BindToken(CancellationToken token)
		{
			_token = token;
			return true;
		}


		TaskCompletionSource<bool> _continue = new TaskCompletionSource<bool>();
		public RelayCommand DoLogout { get { return new RelayCommand(Logout); } }
		public RelayCommand DoDelete { get { return new RelayCommand(Delete); } }
		public RelayCommand DoLogin { get { return new RelayCommand(Login); } }
		public RelayCommand ContinueOAuthCommand { get { return new RelayCommand(() => { _continue.TrySetResult(true); SnooStreamViewModel.NavigationService.GoBack(); }); } }
		public RelayCommand RetryOAuthCommand { get { return new RelayCommand(Login); } }
		public RelayCommand CancelOAuthCommand { get { return new RelayCommand(() => SnooStreamViewModel.NavigationService.GoBack()); } }



		
	}
}
