﻿using Microsoft.HockeyApp;
using SnooStreamBackground;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace SnooStream
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            //Microsoft.HockeyApp.HockeyClient.Current.Configure("787daeede4e140c8876e8cff891543de",
            //    new Microsoft.HockeyApp.TelemetryConfiguration()
            //    {
            //        Collectors = WindowsCollectors.Metadata | WindowsCollectors.Session | WindowsCollectors.UnhandledException
            //    });
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);
            Debug.Assert(MainPage.Current != null, "Main page was not created yet");
            if (args.Kind == ActivationKind.WebAuthenticationBrokerContinuation && MainPage.Current != null)
            {
                var wab = args as WebAuthenticationBrokerContinuationEventArgs;
                await MainPage.Current.NavContext.LoginViewModel.Context.HandleOAuth(wab.WebAuthenticationResult);
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
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
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
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();

            try
            {
                var taskRegistered = false;
                var taskName = "SnooStreamUpdateTask";

                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == taskName)
                    {
                        taskRegistered = true;
                        break;
                    }
                }

                if (Windows.System.UserProfile.UserProfilePersonalizationSettings.IsSupported())
                {

                    //Windows.System.UserProfile.UserProfilePersonalizationSettings.Current.TrySetLockScreenImageAsync()
                }

                if (!taskRegistered)
                {
                    
                    var lockscreenSettings = new LockScreenSettings();
                    if (lockscreenSettings.LiveTileSettings == null || lockscreenSettings.LiveTileSettings.Count == 0)
                    {
                        lockscreenSettings.LiveTileSettings = new List<LiveTileSettings> { new LiveTileSettings() { CurrentImages = new List<string>(), LiveTileStyle = LiveTileStyle.TextImage, LiveTileItemsReddit = "/" } };
                        lockscreenSettings.Store();
                    }


                    var result = await BackgroundExecutionManager.RequestAccessAsync();
                    //
                    // Must be the same entry point that is specified in the manifest.
                    //
                    String taskEntryPoint = typeof(SnooStreamBackground.UpdateBackgroundTask).FullName;

                    //
                    // A time trigger that repeats at 30-minute intervals.
                    //
                    IBackgroundTrigger trigger = new TimeTrigger(30, false);

                    //
                    // Builds the background task.
                    //
                    BackgroundTaskBuilder builder = new BackgroundTaskBuilder();

                    builder.Name = taskName;
                    builder.IsNetworkRequested = true;
                    builder.TaskEntryPoint = taskEntryPoint;
                    builder.SetTrigger(trigger);

                    //
                    // Registers the background task, and get back a BackgroundTaskRegistration object representing the registered task.
                    //
                    BackgroundTaskRegistration task = builder.Register();
                }
            }
            catch (Exception ex)
            {
                //gotta catch em all!
                Debug.WriteLine(ex);
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
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            if (MainPage.Current != null)
            {
                await MainPage.Current.NavContext.SaveState();
            }
            deferral.Complete();
        }
    }
}
