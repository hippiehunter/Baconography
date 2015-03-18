using GalaSoft.MvvmLight;
using Newtonsoft.Json;
using SnooStream.Messages;
using SnooStream.PlatformServices;
using SnooStream.Services;
using SnooStream.ViewModel;
using SnooStreamBackground;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace SnooStream.Common
{
    public class SnooStreamViewModelPlatform : SnooStreamViewModel
    {
        public SnooStreamViewModelPlatform()
        {
            try
            {
                SnooStreamViewModel.SystemServices = new SystemServices();
                SnooStreamViewModel.MarkdownProcessor = new MarkdownProvider();
                SnooStreamViewModel.ActivityManager = new SnooStream.PlatformServices.ActivityManager();
                if (!IsInDesignMode)
                {
                    ((SystemServices)SnooStreamViewModel.SystemServices).FinishInitialization(Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher);
                    SnooStreamViewModel.CWD = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
                    SnooStreamViewModel.UserCredentialService = new DefaultUserCredentialService();

                }
                FinishInit();

                if (!IsInDesignMode)
                {
					MakeHubSections();
					RaisePropertyChanged("HubSections");

                    LockScreenSettings lsSettings = new LockScreenSettings();
                    lsSettings.LiveTileSettings = new List<LiveTileSettings>
                    {
                        new LiveTileSettings { CurrentImages = new List<string>(), LiveTileItemsReddit = "/", LiveTileStyle = LiveTileStyle.TextImage}
                    };
                    lsSettings.RedditOAuth = SnooStreamViewModel.RedditUserState != null && SnooStreamViewModel.RedditUserState.OAuth != null ?
                        JsonConvert.SerializeObject(SnooStreamViewModel.RedditUserState) : "";

                    lsSettings.Store();

					MessengerInstance.Register<UserLoggedInMessage>(this, OnUserLoggedIn);

                    Task.Delay(10000).ContinueWith(async (tskTop) =>
                        {
                            try
                            {
                                await BackgroundExecutionManager.RequestAccessAsync();
                                TimeTrigger timeTrigger = new TimeTrigger(30, false);
                                SystemCondition userCondition = new SystemCondition(SystemConditionType.UserPresent);
                                string entryPoint = "SnooStreamBackground.UpdateBackgroundTask";
                                string taskName = "Background task for updating live tile and displaying message notifications from reddit";

                                BackgroundTaskRegistration task = RegisterBackgroundTask(entryPoint, taskName, timeTrigger, userCondition);
                            }
                            catch (Exception ex)
                            {
                                _logger.Error("failed to register background task", ex);
                            }

                            SystemServices.RunUIIdleAsync(() =>
                                {
                                    return Task.Run(() =>
                                        {
                                            UpdateBackgroundTask tsk = new UpdateBackgroundTask();
                                            try
                                            {
                                                tsk.RunExternal();
                                            }
                                            catch { }
                                        });
                                });
                        }, TaskScheduler.Current);

                }
            }
            catch (Exception ex)
            {
                _logger.Fatal("fetal error during initialization", ex);
                throw ex;
            }
        }

		private void MakeHubSections()
		{
            if (Login.IsMod)
            {
                HubSections = new List<HubSection>
						{
							new HubSection { Header = "subreddit" },
							new HubSection { Header = "activity" },
							new HubSection { Header = "mod" },
							new HubSection { Header = "self" },
							new HubSection { Header = "settings" }
						};
            }
			else if (Login.IsLoggedIn)
			{
				HubSections = new List<HubSection>
						{
							new HubSection { Header = "subreddit" },
							new HubSection { Header = "activity" },
							new HubSection { Header = "self" },
							new HubSection { Header = "settings" }
						};
			}
			else
			{
				HubSections = new List<HubSection>
						{
							new HubSection { Header = "subreddit" },
							new HubSection { Header = "login" },
							new HubSection { Header = "settings" }
						};
			}

			RaisePropertyChanged("HubSections");
		}

		private void OnUserLoggedIn(UserLoggedInMessage obj)
		{
			SnooStreamViewModel.ActivityManager.Clear();
			SnooStreamViewModel.ActivityManager = new SnooStream.PlatformServices.ActivityManager();
			SelfStream.OnUserLoggedIn(obj);
			MakeHubSections();
		}

        public static BackgroundTaskRegistration RegisterBackgroundTask(
                                                string taskEntryPoint,
                                                string name,
                                                IBackgroundTrigger trigger,
                                                IBackgroundCondition condition)
        {

            foreach (var cur in BackgroundTaskRegistration.AllTasks)
            {

                if (cur.Value.Name == name)
                {
                    // 
                    // The task is already registered.
                    // 

                    return (BackgroundTaskRegistration)(cur.Value);
                }
            }

            var builder = new BackgroundTaskBuilder();

            builder.Name = name;
            builder.TaskEntryPoint = taskEntryPoint;
            builder.SetTrigger(trigger);

            if (condition != null)
            {

                builder.AddCondition(condition);
            }

            BackgroundTaskRegistration task = builder.Register();

            return task;
        }

        public override void Suspend()
        {
            _backgroundCancellationTokenSource.Cancel();
            DumpInitBlob(((NavigationService)SnooStreamViewModel.NavigationService).DumpState());
        }

        public override void Resume()
        {
            if (!_backgroundCancellationTokenSource.IsCancellationRequested)
                _backgroundCancellationTokenSource.Cancel();

            _backgroundCancellationTokenSource = new CancellationTokenSource();
            if (SelfStream.IsLoggedIn)
                SelfStream.RunActivityUpdater();
        }

        public override void SetFocusedViewModel(ViewModelBase viewModel)
        {
            SnooApplicationPage.Current.SetFocusedViewModel(viewModel);
        }


		public List<HubSection> HubSections { get; private set; }
		public class HubSection
		{
			public string Header { get; set; }
			public override string ToString()
			{
				return Header;
			}
		}
    }
}
