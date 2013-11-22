﻿using GalaSoft.MvvmLight;
using SnooSharp;
using SnooStream.Common;
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
        public static string CurrentWorkingDirectory { get; set; }
        public SnooStreamViewModel()
        {
            if (CurrentWorkingDirectory == null)
                CurrentWorkingDirectory = "";

            _listingFilter = new NSFWListingFilter();
            OfflineService = new OfflineService(CurrentWorkingDirectory);
            RedditUserState = new UserState();
            RedditService = new Reddit(_listingFilter, RedditUserState, OfflineService, CaptchaProvider);
            _initializationBlob = OfflineService.LoadInitializationBlob("");
            Settings = new Model.Settings(_initializationBlob.Settings);
            _listingFilter.Initialize(Settings, OfflineService, RedditService, _initializationBlob.NSFWFilter);
            CommandDispatcher = new CommandDispatcher();
            UserHub = new UserHubViewModel(_initializationBlob);
            ModeratorHub = new ModeratorHubViewModel();
            SettingsHub = new SettingsViewModel();
            SubredditRiver = new SubredditRiverViewModel();
        }

        private InitializationBlob _initializationBlob;
        private NSFWListingFilter _listingFilter;
        public static CommandDispatcher CommandDispatcher {get; set;}
        public static Settings Settings { get; set; }
        public static OfflineService OfflineService { get; private set; }
        public static UserState RedditUserState { get; private set; }
        public static Reddit RedditService { get; private set; }
        public static ICaptchaProvider CaptchaProvider { get; private set; }
        public static IMarkdownProcessor MarkdownProcessor { get; private set; }
        public static IUserCredentialService UserCredentialService { get; private set; }
        public static INotificationService NotificationService { get; private set; }
        public static INavigationService NavigationService { get; private set; }
        public static ISystemServices SystemServices { get; private set; }

        public UserHubViewModel UserHub { get; private set; }
        public ModeratorHubViewModel ModeratorHub { get; private set; }
        public SettingsViewModel SettingsHub { get; private set; }
        public SubredditRiverViewModel SubredditRiver { get; private set; }

        public UploadViewModel UploadHub { get; private set; }

        public static CancellationToken UIContextCancellationToken { get; set; }
    }
}
