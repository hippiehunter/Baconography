using Newtonsoft.Json;
using SnooStream.PlatformServices;
using SnooStream.View.Pages;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=402347&clcid=0x409

namespace SnooStream
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Allows tracking page views, exceptions and other telemetry through the Microsoft Application Insights service.
        /// </summary>
        public static Microsoft.ApplicationInsights.TelemetryClient TelemetryClient;

        private TransitionCollection transitions;
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            TelemetryClient = new Microsoft.ApplicationInsights.TelemetryClient();

            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);
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
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    var snooStreamViewModel = Application.Current.Resources["SnooStream"] as SnooStreamViewModel;
                    var navService = new NavigationService(rootFrame, snooStreamViewModel);
                    SnooStreamViewModel.NavigationService = navService;
                    navService.Finish(snooStreamViewModel.GetNavigationBlob());
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
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
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

                var snooStreamViewModel = Application.Current.Resources["SnooStream"] as SnooStreamViewModel;
                SnooStreamViewModel.NavigationService = new NavigationService(rootFrame, snooStreamViewModel);
                ((NavigationService)SnooStreamViewModel.NavigationService).Finish(null);
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                DispatchToInitialPage(rootFrame, e.Arguments);
            }
            else if (!string.IsNullOrWhiteSpace(e.Arguments))
            {
                DispatchToInitialPage(rootFrame, e.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

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
                    if (targetActivity != null)
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

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

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
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
