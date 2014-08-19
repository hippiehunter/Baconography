using SnooStream.PlatformServices;
using SnooStream.Services;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
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
		}
    }
}
