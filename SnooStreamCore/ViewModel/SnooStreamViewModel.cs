using CommonResourceAcquisition.ImageAcquisition;
using GalaSoft.MvvmLight;
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
    public class SnooStreamViewModel : ViewModelBase
    {
        public static string CWD { get; set; }

		protected void FinishInit()
		{
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
			RedditUserState = _initializationBlob.DefaultUser ?? new UserState();
			NotificationService = new Common.NotificationService();
			CaptchaProvider = new CaptchaService();
			RedditService = new Reddit(_listingFilter, RedditUserState, OfflineService, CaptchaProvider, "3m9rQtBinOg_rA", null, "http://www.google.com");
			Login = new LoginViewModel();

			_listingFilter.Initialize(Settings, OfflineService, RedditService, _initializationBlob.NSFWFilter);
			CommandDispatcher = new CommandDispatcher();
			SubredditRiver = new SubredditRiverViewModel(_initializationBlob.Subreddits);
			MessengerInstance.Register<UserLoggedInMessage>(this, OnUserLoggedIn);
		}

        private void OnUserLoggedIn(UserLoggedInMessage obj)
        {
            if (obj.IsDefault)
            {
                _initializationBlob.DefaultUser = RedditUserState;
            }
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

		public LoginViewModel Login { get; private set; }
        public SettingsViewModel SettingsHub { get; private set; }
        public SubredditRiverViewModel SubredditRiver { get; private set; }

        public UploadViewModel UploadHub { get; private set; }
        public string FeaturedImage { get; private set; }

		public static SnooStream.Common.LoggingService Logging = new LoggingService();
        private static CancellationTokenSource _uiContextCancellationSource = new CancellationTokenSource();
        public static CancellationToken UIContextCancellationToken
        {
            get
            {
                return _uiContextCancellationSource.Token;
            }
        }

        private static CancellationTokenSource _backgroundCancellationTokenSource = new CancellationTokenSource();
        public static CancellationToken BackgroundCancellationToken
        {
            get
            {
                return _backgroundCancellationTokenSource.Token;
            }
        }

        public void DumpInitBlob(string navigationBlob = null)
        {
            _initializationBlob.Settings = Settings.Dump();
            //_initializationBlob.Self = UserHub.Self.Dump();

			if(RedditUserState.IsDefault)
				_initializationBlob.DefaultUser = RedditUserState;

			_initializationBlob.NavigationBlob = navigationBlob;
			_initializationBlob.Subreddits = SubredditRiver.Dump();
            OfflineService.StoreInitializationBlob(_initializationBlob);
        }

		public string GetNavigationBlob() {  return _initializationBlob.NavigationBlob; }
    }
}
