using MetroLog;
using MetroLog.Targets;
using Newtonsoft.Json;
using SnooSharp;
using SnooStream.Common;
using SnooStream.Messages;
using SnooStream.PlatformServices;
using SnooStream.Services;
using SnooStream.View.Pages;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace SnooStream
{
    public sealed partial class App : Application
    {
#if WINDOWS_PHONE_APP
        private TransitionCollection transitions;
#endif
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            LogManagerFactory.DefaultConfiguration.AddTarget(LogLevel.Trace, LogLevel.Fatal, new FileStreamingTarget());
            this.InitializeComponent();
            this.Suspending += this.OnSuspending;
            this.Resuming += App_Resuming;

            // setup the global crash handler...
            GlobalCrashHandler.Configure();
        }

        private void App_Resuming(object sender, object e)
        {
            var snooStreamViewModel = Resources["SnooStream"] as SnooStreamViewModel;
            snooStreamViewModel.Resume();
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);
#if WINDOWS_PHONE_APP
			if (args.Kind == ActivationKind.WebAuthenticationBrokerContinuation)
			{
				var wab = args as WebAuthenticationBrokerContinuationEventArgs;
				if (wab.WebAuthenticationResult.ResponseStatus == Windows.Security.Authentication.Web.WebAuthenticationStatus.Success)
				{
					var resultData = wab.WebAuthenticationResult.ResponseData;
					var decoder = new WwwFormUrlDecoder(new Uri(resultData).Query);
					var code = decoder.GetFirstValueByName("code");
					var snooStreamViewModel = Application.Current.Resources["SnooStream"] as SnooStreamViewModel;
					snooStreamViewModel.Login.FinishOAuth(code);
				}
				else
				{
					var snooStreamViewModel = Application.Current.Resources["SnooStream"] as SnooStreamViewModel;
					snooStreamViewModel.Login.FailOAuth(wab.WebAuthenticationResult.ResponseStatus.ToString(), wab.WebAuthenticationResult.ResponseData);
				}
			}
            else if (args.Kind == ActivationKind.Launch)
            {

            }
#endif
        }
        Timer timer;
        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
#if WINDOWS_PHONE_APP
                //timer = new Timer((state) =>
                //{
                //    Debug.WriteLine(Windows.System.MemoryManager.AppMemoryUsageLimit.ToString() + " | " +
                //        Windows.System.MemoryManager.AppMemoryUsage.ToString());
                //}, null, 0, 500);
#endif
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

#if WINDOWS_PHONE_APP
                rootFrame.CacheSize = MemoryManager.AppMemoryUsageLimit > 300 * 1024 * 1024 ? 2 : 2;
#else
                rootFrame.CacheSize = 4;
#endif

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    var snooStreamViewModel = Application.Current.Resources["SnooStream"] as SnooStreamViewModel;
                    var navService = new NavigationService(rootFrame, snooStreamViewModel);
                    SnooStreamViewModel.NavigationService = navService;
                    navService.Finish(snooStreamViewModel.GetNavigationBlob());
#if WINDOWS_PHONE_APP
					// Removes the turnstile navigation for startup.
					if (rootFrame.ContentTransitions != null)
					{
						this.transitions = new TransitionCollection();
						foreach (var c in rootFrame.ContentTransitions)
						{
							this.transitions.Add(c);
						}
					}

					rootFrame.ContentTransitions = null;
					rootFrame.Navigated += this.RootFrame_FirstNavigated;
					Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
#endif
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
#if WINDOWS_PHONE_APP
                // Removes the turnstile navigation for startup.
                if (rootFrame.ContentTransitions != null)
                {
                    this.transitions = new TransitionCollection();
                    foreach (var c in rootFrame.ContentTransitions)
                    {
                        this.transitions.Add(c);
                    }
                }

                rootFrame.ContentTransitions = null;
                rootFrame.Navigated += this.RootFrame_FirstNavigated;
                Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
#endif
                var snooStreamViewModel = Application.Current.Resources["SnooStream"] as SnooStreamViewModel;
                SnooStreamViewModel.NavigationService = new NavigationService(rootFrame, snooStreamViewModel);
                ((NavigationService)SnooStreamViewModel.NavigationService).Finish(null);
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                DispatchToInitialPage(rootFrame, e.Arguments);
                
            }
            else if(!string.IsNullOrWhiteSpace(e.Arguments))
            {
                DispatchToInitialPage(rootFrame, e.Arguments);
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }

        void DispatchToInitialPage(Frame rootFrame, string launchArgs)
        {
            if (string.IsNullOrWhiteSpace(launchArgs))
            {
                if (!rootFrame.Navigate(typeof(SnooHubMark2), launchArgs))
                {
                    throw new Exception("Failed to create initial page");
                }
            }
            else
            {
                try
                {
                    var activityParams = JsonConvert.DeserializeAnonymousType(launchArgs, new { activityid = "" });
                    var targetActivity = SelfStreamViewModel.ActivityLookup.ContainsKey(activityParams.activityid) ? SelfStreamViewModel.ActivityLookup[activityParams.activityid] : null;
                    if(targetActivity != null)
                        targetActivity.Tapped();
                }
                catch (Exception)
                {
                    if (!rootFrame.Navigate(typeof(SnooHubMark2), launchArgs))
                    {
                        throw new Exception("Failed to create initial page");
                    }
                }
            }
            
        }

#if WINDOWS_PHONE_APP
        /// <summary>
        /// Restores the content transitions after the app has launched.
        /// </summary>
        /// <param name="sender">The object where the handler is attached.</param>
        /// <param name="e">Details about the navigation event.</param>
        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            var rootFrame = sender as Frame;
            rootFrame.ContentTransitions = this.transitions ?? new TransitionCollection() { new NavigationThemeTransition() };
            rootFrame.Navigated -= this.RootFrame_FirstNavigated;
        }

		void HardwareButtons_BackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
		{
			var rootFrame = Window.Current.Content as Frame;
            var appPage = rootFrame.Content as SnooApplicationPage;
            if(appPage.PopNavState())
            {
                e.Handled = true;
            }
			else if (rootFrame.CanGoBack)
			{
				rootFrame.GoBack();
				//Indicate the back button press is handled so the app does not exit
				e.Handled = true;
			}
            else
            {
                var snooStreamViewModel = Application.Current.Resources["SnooStream"] as SnooStreamViewModel;
                snooStreamViewModel.DumpInitBlob();
            }
            
		}
#endif

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            var snooStreamViewModel = Application.Current.Resources["SnooStream"] as SnooStreamViewModel;
            snooStreamViewModel.Suspend();
            // TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}