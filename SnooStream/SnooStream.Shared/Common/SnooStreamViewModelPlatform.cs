using Newtonsoft.Json;
using SnooStream.PlatformServices;
using SnooStream.Services;
using SnooStream.ViewModel;
using SnooStreamBackground;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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
                    new LiveTileSettings { CurrentImages = new List<string>(), LiveTileItemsReddit = "/", LiveTileStyle = LiveTileStyle.Default}
                };
                lsSettings.RedditOAuth = SnooStreamViewModel.RedditUserState != null && SnooStreamViewModel.RedditUserState.OAuth != null ?
                    JsonConvert.SerializeObject(SnooStreamViewModel.RedditUserState.OAuth) : "";
                lsSettings.Store();

                Task.Delay(10000).ContinueWith((tskTop) => 
                    {
                        SystemServices.RunUIIdleAsync(() =>
                            {
                                UpdateBackgroundTask tsk = new UpdateBackgroundTask();
                                tsk.RunExternal();
                                return Task.FromResult(true);
                            });
                    });
                
            }
		}
    }
}
