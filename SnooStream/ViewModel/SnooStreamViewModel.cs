using CommonImageAquisition;
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
        public SnooStreamViewModel()
        {
            UIScheduler = TaskScheduler.FromCurrentSynchronizationContext();
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
            RedditService = new Reddit(_listingFilter, RedditUserState, OfflineService, CaptchaProvider);
            LoadQueue = new PriorityLoadQueue();


            _listingFilter.Initialize(Settings, OfflineService, RedditService, _initializationBlob.NSFWFilter);
            CommandDispatcher = new CommandDispatcher();
            UserHub = new UserHubViewModel(_initializationBlob.Self);
            ModeratorHub = new ModeratorHubViewModel();
            SettingsHub = new SettingsViewModel();
            SubredditRiver = new SubredditRiverViewModel(_initializationBlob.Subreddits);
            if (!IsInDesignMode)
            {
                LoadLargeImages();
            }
            MessengerInstance.Register<UserLoggedInMessage>(this, OnUserLoggedIn);
        }

        private void OnUserLoggedIn(UserLoggedInMessage obj)
        {
            if (obj.IsDefault)
            {
                _initializationBlob.DefaultUser = RedditUserState;
            }
        }

        private async void LoadLargeImages()
        {
            await Task.Delay(1000); //stay away from startup, we've got enough going on as it is
            if (SystemServices.IsLowPriorityNetworkOk)
            {
                await NotificationService.Report("loading secondary images",
                    PriorityLoadQueue.QueueHelper("main", LoadContextType.Major, async () =>
                {
                    //check if there is a LinkRiver for the target subreddit, then cache things 
                    //into it directly so we arent making 2x the reddit calls
                    var targetRiver = SubredditRiver.CombinedRivers.FirstOrDefault(lrvm => string.Compare(lrvm.Thing.Url, Settings.LockScreenReddit, StringComparison.CurrentCultureIgnoreCase) == 0);
                    if (targetRiver != null)
                    {
                        targetRiver = new LinkRiverViewModel(true, new Subreddit(Settings.LockScreenReddit), "hot", null);
                    }


                    //var loadedContent = await targetRiver.PreloadContent((link) => ImageAquisition.MightHaveImagesFromUrl(link.Link.Url) && !link.Link.Url.EndsWith(".gif"), 12, BackgroundCancellationToken);
                    //foreach (var content in loadedContent.OfType<ImageViewModel>())
                    //{
                    //    if (content.ImageSource.Dimensions != null)
                    //    {

                    //    }
                    //}
                }));

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
        public static PriorityLoadQueue LoadQueue { get; set; }

        public UserHubViewModel UserHub { get; private set; }
        public ModeratorHubViewModel ModeratorHub { get; private set; }
        public SettingsViewModel SettingsHub { get; private set; }
        public SubredditRiverViewModel SubredditRiver { get; private set; }

        public UploadViewModel UploadHub { get; private set; }
        public string FeaturedImage { get; private set; }

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
            _initializationBlob.Self = UserHub.Self.Dump();
			_initializationBlob.NavigationBlob = navigationBlob;
            OfflineService.StoreInitializationBlob(_initializationBlob);
        }

		public string GetNavigationBlob() {  return _initializationBlob.NavigationBlob; }

        public static TaskScheduler UIScheduler { get; set; }
    }
}
