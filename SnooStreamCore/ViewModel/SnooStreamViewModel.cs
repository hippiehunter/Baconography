using CommonResourceAcquisition.ImageAcquisition;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;
using SnooSharp;
using SnooStream.Common;
using SnooStream.Messages;
using SnooStream.Model;
using SnooStream.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public abstract class SnooStreamViewModel : ViewModelBase
    {
        public static string CWD { get; set; }
        protected void FinishInit()
        {
            Current = this;
            _listingFilter = new NSFWListingFilter();
            if (IsInDesignMode)
            {
                _initializationBlob = new InitializationBlob { Settings = new Dictionary<string, string>(), NSFWFilter = new Dictionary<string, bool>() };
            }
            else
            {
                OfflineService = new OfflineService();
                _initializationBlob = OfflineService.LoadInitializationBlob("");
            }
            Settings = new Model.Settings(_initializationBlob.Settings);
            SettingsHub = new SettingsViewModel(Settings);

            RedditUserState = _initializationBlob.DefaultUser ?? new UserState();

            SnooStreamViewModel.ActivityManager.OAuth = SnooStreamViewModel.RedditUserState != null && SnooStreamViewModel.RedditUserState.OAuth != null ?
                    JsonConvert.SerializeObject(SnooStreamViewModel.RedditUserState) : "";

            SnooStreamViewModel.ActivityManager.CanStore = SnooStreamViewModel.RedditUserState != null && SnooStreamViewModel.RedditUserState.IsDefault;

            NotificationService = new Common.NotificationService();
            CaptchaProvider = new CaptchaService();
            RedditService = new Reddit(_listingFilter, RedditUserState, OfflineService, CaptchaProvider, "3m9rQtBinOg_rA", null, "http://www.google.com");
            Login = new LoginViewModel();

            _listingFilter.Initialize(Settings, OfflineService, RedditService, _initializationBlob.NSFWFilter);
            CommandDispatcher = new CommandDispatcher();
            SubredditRiver = new SubredditRiverViewModel(_initializationBlob.Subreddits);
            SelfStream = new SelfStreamViewModel();
            ModStream = new ModStreamViewModel();
            NavMenu = new NavMenu(Enumerable.Empty<LinkRiverViewModel>(), this);
            MessengerInstance.Register<UserLoggedInMessage>(this, OnUserLoggedIn);

            if (RedditUserState.Username != null)
            {
                SelfUser = new AboutUserViewModel(RedditUserState.Username);
            }
        }

        private void OnUserLoggedIn(UserLoggedInMessage obj)
        {
            if (obj.IsDefault)
            {
                _initializationBlob.DefaultUser = RedditUserState;
            }

            SelfUser = new AboutUserViewModel(obj.NewAccount, DateTime.UtcNow);
            RaisePropertyChanged("SelfUser");
            //_logger.Info("user logged in " + RedditUserState.Username);
        }

        private InitializationBlob _initializationBlob;
        private NSFWListingFilter _listingFilter;
        public static CommandDispatcher CommandDispatcher { get; set; }
        public static Settings Settings { get; set; }
        public static OfflineService OfflineService { get; private set; }
        public static UserState RedditUserState { get; private set; }
        public static Reddit RedditService { get; private set; }
        public static NotificationService NotificationService { get; private set; }
        public static CaptchaService CaptchaProvider { get; set; }
        public static IMarkdownProcessor MarkdownProcessor { get; set; }
        public static IUserCredentialService UserCredentialService { get; set; }
        public static INavigationService NavigationService { get; set; }
        public static ISystemServices SystemServices { get; set; }
        public static IActivityManager ActivityManager { get; set; }

        public AboutUserViewModel SelfUser { get; private set; }
        public SelfStreamViewModel SelfStream { get; private set; }
        public ModStreamViewModel ModStream { get; private set; }
        public LoginViewModel Login { get; private set; }
        public SettingsViewModel SettingsHub { get; private set; }
        public SubredditRiverViewModel SubredditRiver { get; private set; }
        public UploadViewModel UploadHub { get; private set; }
        public NavMenu NavMenu { get; private set; }
        public string FeaturedImage { get; private set; }

		public static SnooStream.Common.LoggingService Logging = new LoggingService();
        protected static CancellationTokenSource _uiContextCancellationSource = new CancellationTokenSource();
        public static CancellationToken UIContextCancellationToken
        {
            get
            {
                return _uiContextCancellationSource.Token;
            }
        }

        protected static CancellationTokenSource _backgroundCancellationTokenSource = new CancellationTokenSource();
        public static CancellationToken BackgroundCancellationToken
        {
            get
            {
                return _backgroundCancellationTokenSource.Token;
            }
        }

        public void DumpInitBlob(string navigationBlob = null)
        {
			//_logger.Info("dumping init blob");
            _initializationBlob.Settings = Settings.Dump();

			if(RedditUserState.IsDefault)
				_initializationBlob.DefaultUser = RedditUserState;

			_initializationBlob.NavigationBlob = navigationBlob;
			_initializationBlob.Subreddits = SubredditRiver.Dump();
            OfflineService.StoreInitializationBlob(_initializationBlob);
			OfflineService.StoreHistory();
			//_logger.Info("dump init blob finished");
        }

		public string GetNavigationBlob() {  return _initializationBlob.NavigationBlob; }
        public void ClearNavigationBlob() { _initializationBlob.NavigationBlob = null; }
        public abstract void Suspend();
        public abstract void Resume();
        public abstract void SetFocusedViewModel(ViewModelBase viewModel);

        public static SnooStreamViewModel Current { get; private set; }
    }
}
