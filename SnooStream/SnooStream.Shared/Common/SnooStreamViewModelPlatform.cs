using Newtonsoft.Json;
using SnooStream.PlatformServices;
using SnooStream.Services;
using SnooStream.ViewModel;
using SnooStreamBackground;
using System;
using System.Collections.Generic;
using System.Text;
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
			SnooStreamViewModel.SystemServices = new SystemServices();
			SnooStreamViewModel.MarkdownProcessor = new MarkdownProvider();

			if (!IsInDesignMode)
			{
				((SystemServices)SnooStreamViewModel.SystemServices).FinishInitialization(Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher);
				SnooStreamViewModel.CWD = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
				SnooStreamViewModel.UserCredentialService = new DefaultUserCredentialService();
			}
			FinishInit();

            if (!IsInDesignMode)
			{
                LockScreenSettings lsSettings = new LockScreenSettings();
                lsSettings.LiveTileSettings = new List<LiveTileSettings>
                {
                    new LiveTileSettings { CurrentImages = new List<string>(), LiveTileItemsReddit = "/", LiveTileStyle = LiveTileStyle.Image}
                };
                lsSettings.RedditOAuth = SnooStreamViewModel.RedditUserState != null && SnooStreamViewModel.RedditUserState.OAuth != null ?
                    JsonConvert.SerializeObject(SnooStreamViewModel.RedditUserState) : "";
                lsSettings.Store();

                Task.Delay(10000).ContinueWith(async (tskTop) => 
                    {
                        var status = BackgroundExecutionManager.GetAccessStatus();
                        if (status == BackgroundAccessStatus.Unspecified)
                        {
                            status = await BackgroundExecutionManager.RequestAccessAsync();
                        }

                        if (status != BackgroundAccessStatus.Denied)
                        {
                            TimeTrigger timeTrigger = new TimeTrigger(30, false);
                            SystemCondition userCondition = new SystemCondition(SystemConditionType.UserPresent);
                            string entryPoint = "SnooStreamBackground.UpdateBackgroundTask";
                            string taskName = "Background task for updating live tile and displaying message notifications from reddit";

                            BackgroundTaskRegistration task = RegisterBackgroundTask(entryPoint, taskName, timeTrigger, userCondition);
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
                    });
                
            }
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
    }
}
