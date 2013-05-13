﻿using System;
using System.Diagnostics;
using System.Resources;
using System.Windows;
using System.Windows.Markup;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BaconographyWP8.Resources;
using BaconographyPortable.Services;
using BaconographyWP8.PlatformServices;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Threading;
using Microsoft.Phone.Info;
using System.Windows.Media;
using GalaSoft.MvvmLight.Messaging;
using BaconographyWP8.Messages;

namespace BaconographyWP8
{
    public partial class App : Application
    {
        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public static PhoneApplicationFrame RootFrame { get; private set; }

		private BaconProvider _baconProvider;
		private NavigationServices navigator;

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions.
            UnhandledException += Application_UnhandledException;

			// Bacon-specific initialization
			InitializeBacon();

            // Standard XAML initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            // Language display initialization
            InitializeLanguage();

			navigator = new NavigationServices();
			navigator.Init(RootFrame);

            // Show graphics profiling information while debugging.
            if (Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode,
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Prevent the screen from turning off while under the debugger by disabling
                // the application's idle detection.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
                //Application.Current.Host.Settings.EnableCacheVisualization = true;
                //Application.Current.Host.Settings.EnableRedrawRegions = true;
            }

        }

        public static class LowMemoryHelper
        {
            private static Timer timer = null;

            public static void BeginRecording()
            {
                // before we start recording we can clean up the previous session.
                // e.g. Get a logging file from IsoStore and upload to the server 

                // start a timer to report memory conditions every 2 seconds
                timer = new Timer(state =>
                {
                    // every 2 seconds do something 
                    string report =
                        DateTime.Now.ToLongTimeString() + " memory conditions: " +
                        Environment.NewLine +
                        "\tApplicationCurrentMemoryUsage: " +
                            DeviceStatus.ApplicationCurrentMemoryUsage +
                            Environment.NewLine +
                        "\tApplicationPeakMemoryUsage: " +
                            DeviceStatus.ApplicationPeakMemoryUsage +
                            Environment.NewLine +
                        "\tApplicationMemoryUsageLimit: " +
                            DeviceStatus.ApplicationMemoryUsageLimit +
                            Environment.NewLine +
                        "\tDeviceTotalMemory: " + DeviceStatus.DeviceTotalMemory + Environment.NewLine +
                        "\tApplicationWorkingSetLimit: " +
                            DeviceExtendedProperties.GetValue("ApplicationWorkingSetLimit") +
                            Environment.NewLine;

                    // write to IsoStore or debug conolse
                    Debug.WriteLine(report);
                },
                    null,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(2));
            }
        }

		private void InitializeBacon()
		{
			if (_baconProvider == null)
			{
				_baconProvider = new BaconProvider();
				_baconProvider.AddService(typeof(IDynamicViewLocator), new DynamicViewLocator());

				_baconProvider.Initialize(RootFrame);

				ViewModelLocator.Initialize(_baconProvider);
			}
			else
			{
				_baconProvider.Initialize(RootFrame);
			}
		}

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            LowMemoryHelper.BeginRecording();
			InitializeBacon();
            
			if (RootFrame.Content == null)
			{
				// When the navigation stack isn't restored navigate to the first page,
				// configuring the new page by passing required information as a navigation
				// parameter
				/*if (!navigator.Navigate(_baconProvider.GetService<IDynamicViewLocator>().RedditView, null))
				{
					throw new Exception("Failed to create initial page");
				}*/
			}
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;


        private object _backgroundColorResource;
        private object _accentColorResource;
        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new TransitionFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;
            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;

        }


        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion

        // Initialize the app's font and flow direction as defined in its localized resource strings.
        //
        // To ensure that the font of your application is aligned with its supported languages and that the
        // FlowDirection for each of those languages follows its traditional direction, ResourceLanguage
        // and ResourceFlowDirection should be initialized in each resx file to match these values with that
        // file's culture. For example:
        //
        // AppResources.es-ES.resx
        //    ResourceLanguage's value should be "es-ES"
        //    ResourceFlowDirection's value should be "LeftToRight"
        //
        // AppResources.ar-SA.resx
        //     ResourceLanguage's value should be "ar-SA"
        //     ResourceFlowDirection's value should be "RightToLeft"
        //
        // For more info on localizing Windows Phone apps see http://go.microsoft.com/fwlink/?LinkId=262072.
        //
        private void InitializeLanguage()
        {
            try
            {
                // Set the font to match the display language defined by the
                // ResourceLanguage resource string for each supported language.
                //
                // Fall back to the font of the neutral language if the Display
                // language of the phone is not supported.
                //
                // If a compiler error is hit then ResourceLanguage is missing from
                // the resource file.
                RootFrame.Language = XmlLanguage.GetLanguage(AppResources.ResourceLanguage);

                // Set the FlowDirection of all elements under the root frame based
                // on the ResourceFlowDirection resource string for each
                // supported language.
                //
                // If a compiler error is hit then ResourceFlowDirection is missing from
                // the resource file.
                FlowDirection flow = (FlowDirection)Enum.Parse(typeof(FlowDirection), AppResources.ResourceFlowDirection);
                RootFrame.FlowDirection = flow;
            }
            catch
            {
                // If an exception is caught here it is most likely due to either
                // ResourceLangauge not being correctly set to a supported language
                // code or ResourceFlowDirection is set to a value other than LeftToRight
                // or RightToLeft.

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                throw;
            }
        }
    }
}