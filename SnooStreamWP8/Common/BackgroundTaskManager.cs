using CommonImageAcquisition;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Scheduler;
using SnooSharp;
using SnooStream.Model;
using SnooStream.TaskSettings;
using SnooStream.ViewModel;
using SnooStreamWP8.BackgroundControls.View;
using SnooStreamWP8.BackgroundControls.ViewModel;
using SnooStreamWP8.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Networking.Connectivity;

namespace SnooStreamWP8.Common
{
    class BackgroundTaskManager
    {
        public static async Task<bool> StartLockScreenProvider()
        {
            var granted = await RequestLockAccess();
            if (!granted)
                return false;

            if (SnooStreamViewModel.Settings.EnableUpdates)
                StartPeriodicAgent();

            if (SnooStreamViewModel.Settings.EnableOvernightUpdates)
                StartIntensiveAgent();

            SetRandomLockScreen();

            return true;
        }

        public static async Task<bool> MaybeUpdateLockScreenImages()
        {
            var taskSettings = TaskSettingsLoader.LoadTaskSettings();
            if (!_imageLoadInProgress && taskSettings.LockScreenImageURIs.Count <= 1)
                return await UpdateLockScreenImages();
            return false;
        }

        static bool _imageLoadInProgress = false;
        public static async Task<bool> UpdateLockScreenImages(int limit = 10)
        {
            if (_imageLoadInProgress)
            {
                while (_imageLoadInProgress)
                    await Task.Yield();

                return _imageLoadInProgress;
            }

            if (!CanDownload)
                return _imageLoadInProgress;

            _imageLoadInProgress = true;

            try
            {
                List<string> results = new List<string>();
                var imagesSubredditResult = await SnooStreamViewModel.RedditService.GetPostsBySubreddit(
                    Utility.CleanRedditLink(SnooStreamViewModel.Settings.ImagesSubreddit, SnooStreamViewModel.RedditUserState.Username),
                    "hot", 100);

                var imageLinks = imagesSubredditResult.Data.Children;

                imageLinks.Select(thing => thing.Data is Link && ImageAcquisition.IsImage(((Link)thing.Data).Url)).ToList();

                if (imageLinks.Count <= 0)
                    // Couldn't load images error
                    return _imageLoadInProgress;

                Shuffle(imageLinks);

                for (int i = 0; i < imageLinks.Count; i++)
                {
                    if (!(imageLinks[i].Data is Link))
                        continue;

                    try
                    {
                        var url = ((Link)imageLinks[i].Data).Url;
                        BitmapImage imageSource = new BitmapImage();
                        imageSource.CreateOptions = BitmapCreateOptions.None;

                        var imagesList = await ImageAcquisition.GetImagesFromUrl("", url);
                        if (imagesList == null || imagesList.Count() == 0)
                            continue;

                        url = imagesList.First().Item2;

                        using (var stream = await PlatformImageAcquisition.ImageStreamFromUrl(url))
                        {
                            try
                            {
                                if (url.EndsWith(".jpg") || url.EndsWith(".jpeg"))
                                {
                                    var dimensions = JpegUtility.GetJpegDimensions(stream);
                                    stream.Seek(0, SeekOrigin.Begin);
                                    //bigger than 16 megs when loaded means we need to chuck it
                                    if (dimensions == null || (dimensions.Height * dimensions.Width * 4) > 16 * 1024 * 1024)
                                        continue;
                                }
                                else if (stream.Length > 1024 * 1024) //its too big drop it
                                {
                                    continue;
                                }
                            }
                            catch
                            {
                                if (stream.Length > 1024 * 1024) //its too big drop it
                                {
                                    continue;
                                }
                            }

                            imageSource.SetSource(stream);
                        }

                        if (imageSource.PixelHeight == 0 || imageSource.PixelWidth == 0)
                            continue;

                        if (imageSource.PixelHeight < 800
                                || imageSource.PixelWidth < 480)
                            continue;

                        MakeSingleLockScreenFromImage(results.Count, imageSource);
                        //this can happen when the user is still trying to use the application so dont lock up the UI thread with this work
                        await Task.Yield();
                        results.Add(string.Format("lockScreenCache{0}.jpg", results.Count.ToString()));

                        if (results.Count >= limit)
                            break;
                    }
                    catch (OutOfMemoryException oom)
                    {
                        // Ouch
                    }
                }
                var taskSettings = TaskSettingsLoader.LoadTaskSettings();
                taskSettings.LockScreenImageURIs = results;
                taskSettings.SaveTaskSettings();

                SetRandomLockScreen();
            }
            catch
            {

            }
            finally
            {
                _imageLoadInProgress = false;
            }

            return _imageLoadInProgress;
        }

        public static void SetRandomLockScreen()
        {
            if (IsLockScreenProvider)
            {
                TaskSettings settings = TaskSettingsLoader.LoadTaskSettings();
                string imageUrl = "";
                // ms-appx points to the Local app install folder, to reference resources bundled in the XAP package.
                // ms-appdata points to the root of the local app data folder.
                if (settings.LockScreenImageURIs.Count <= 0)
                {
                    imageUrl = "ms-appx:///Assets/RainbowGlass.jpg";
                }
                else
                {
                    Shuffle(settings.LockScreenImageURIs);
                    imageUrl = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\" + settings.LockScreenImageURIs.First();
                }
                
                var uri = new Uri(imageUrl, UriKind.Absolute);

                // Set the lock screen background image.
                Windows.Phone.System.UserProfile.LockScreen.SetImageUri(uri);
            }
        }

        public static bool IsLockScreenProvider
        {
            get
            {
                return Windows.Phone.System.UserProfile.LockScreenManager.IsProvidedByCurrentApplication;
            }
        }

        public static bool CanDownload
        {
            get
            {
                var connectionCostType = NetworkInformation.GetInternetConnectionProfile().GetConnectionCost().NetworkCostType;

                if ((SnooStreamViewModel.Settings.UpdateImagesOnlyOnWifi && Microsoft.Phone.Net.NetworkInformation.DeviceNetworkInformation.IsWiFiEnabled)
                    || (!SnooStreamViewModel.Settings.UpdateImagesOnlyOnWifi && connectionCostType != NetworkCostType.Variable))
                    return true;

                return false;
            }
        }

        public static bool DownloadOptimal
        {
            get
            {
                var connectionCostType = NetworkInformation.GetInternetConnectionProfile().GetConnectionCost().NetworkCostType;

                if ((SnooStreamViewModel.Settings.UpdateImagesOnlyOnWifi && Microsoft.Phone.Net.NetworkInformation.DeviceNetworkInformation.IsWiFiEnabled)
                    || (!SnooStreamViewModel.Settings.UpdateImagesOnlyOnWifi && connectionCostType == NetworkCostType.Unrestricted))
                    return true;

                return false;
            }
        }

        public static readonly string periodicTaskName = "SnooStream_LockScreen_Updater";
        public static readonly string intensiveTaskName = "SnooStream_Intensive_Updater";

        public static void RemoveAgent(string name)
        {
            try
            {
                ScheduledActionService.Remove(name);
            }
            catch (Exception)
            {
            }
        }

        public static void StartPeriodicAgent()
        {
            // Obtain a reference to the period task, if one exists
            var periodicTask = ScheduledActionService.Find(periodicTaskName) as PeriodicTask;

            // If the task already exists and background agents are enabled for the
            // application, you must remove the task and then add it again to update 
            // the schedule

            var disableBackground = SnooStreamViewModel.Settings.DisableBackground;

            if (periodicTask != null)
            {
                if (periodicTask.LastExitReason == AgentExitReason.None && periodicTask.IsScheduled && !disableBackground)
                {
                    return;
                }

                RemoveAgent(periodicTaskName);
            }

            if (disableBackground)
                return;

            periodicTask = new PeriodicTask(periodicTaskName);
            // The description is required for periodic agents. This is the string that the user
            // will see in the background services Settings page on the device.
            periodicTask.Description = "Keeps your lockscreen up to date with the latest redditing";

            // Place the call to Add in a try block in case the user has disabled agents.
            try
            {
                ScheduledActionService.Add(periodicTask);
                //ScheduledActionService.LaunchForTest(periodicTaskName, TimeSpan.FromSeconds(20));
            }
            catch (InvalidOperationException exception)
            {
                if (exception.Message.Contains("BNS Error: The action is disabled"))
                {
                    MessageBox.Show("Background agents for this application have been disabled by the user.");
                }

                if (exception.Message.Contains("BNS Error: The maximum number of ScheduledActions of this type have already been added."))
                {
                    // No user action required. The system prompts the user when the hard limit of periodic tasks has been reached.

                }
            }
            catch (SchedulerServiceException)
            {
            }
        }

        public static void StartIntensiveAgent()
        {
            // Obtain a reference to the period task, if one exists
            var intensiveTask = ScheduledActionService.Find(intensiveTaskName) as ResourceIntensiveTask;

            // If the task already exists and background agents are enabled for the
            // application, you must remove the task and then add it again to update 
            // the schedule
            if (intensiveTask != null)
            {
                RemoveAgent(intensiveTaskName);
            }

            if (SnooStreamViewModel.Settings.DisableBackground)
                return;

            intensiveTask = new ResourceIntensiveTask(intensiveTaskName);
            // The description is required for periodic agents. This is the string that the user
            // will see in the background services Settings page on the device.
            intensiveTask.Description = "This task does all of the heavy lifting for the lock screen updater and overnight offlining support";

            // Place the call to Add in a try block in case the user has disabled agents.
            try
            {
                ScheduledActionService.Add(intensiveTask);
                //ScheduledActionService.LaunchForTest(intensiveTaskName, TimeSpan.FromSeconds(60));
            }
            catch (InvalidOperationException exception)
            {
                if (exception.Message.Contains("BNS Error: The action is disabled"))
                {
                    MessageBox.Show("Background agents for this application have been disabled by the user.");
                }

                if (exception.Message.Contains("BNS Error: The maximum number of ScheduledActions of this type have already been added."))
                {
                    // No user action required. The system prompts the user when the hard limit of periodic tasks has been reached.

                }
            }
            catch (SchedulerServiceException)
            {
            }
        }

        public static async Task<bool> RequestLockAccess()
        {
            try
            {
                var isProvider = Windows.Phone.System.UserProfile.LockScreenManager.IsProvidedByCurrentApplication;
                if (!IsLockScreenProvider)
                {
                    // If you're not the provider, this call will prompt the user for permission.
                    // Calling RequestAccessAsync from a background agent is not allowed.
                    var op = await Windows.Phone.System.UserProfile.LockScreenManager.RequestAccessAsync();

                    // Only do further work if the access was granted.
                    isProvider = op == Windows.Phone.System.UserProfile.LockScreenRequestResult.Granted;
                }

                return isProvider;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

            return false;
        }

        public static void MakeSingleLockScreenFromImage(int pos, BitmapImage imageSource)
        {
            Image lockScreenView = new Image();
            lockScreenView.Width = 480;
            lockScreenView.Height = 800;
            lockScreenView.Source = imageSource;
            lockScreenView.Stretch = Stretch.UniformToFill;
            lockScreenView.UpdateLayout();
            lockScreenView.Measure(new Size(480, 800));
            lockScreenView.Arrange(new Rect(0, 0, 480, 800));
            WriteableBitmap bitmap = new WriteableBitmap(480, 800);
            bitmap.Render(lockScreenView, new ScaleTransform() { ScaleX = 1, ScaleY = 1 });
            bitmap.Invalidate();

            using (var theFile = File.Create(Windows.Storage.ApplicationData.Current.LocalFolder.Path + string.Format("\\lockScreenCache{0}.jpg", pos.ToString())))
            {
                bitmap.SaveJpeg(theFile, 480, 800, 0, 100);
                theFile.Flush(true);
                theFile.Close();
            }
        }

        public async Task<SnooStreamWP8.BackgroundControls.ViewModel.LockScreenViewModel> MakeLockScreenControl(IEnumerable<string> lockScreenImages)
        {
            var user = SnooStreamViewModel.RedditUserState;

            LinkGlyphConverter linkGlyphConverter = new LinkGlyphConverter();
            List<LockScreenMessage> lockScreenMessages = new List<LockScreenMessage>();

            if (SnooStreamViewModel.Settings.PostsInLockScreenOverlay && SnooStreamViewModel.Settings.OverlayItemCount > 0)
            {
                //call for posts from selected subreddit (defaults to front page)
                var frontPageResult = new List<Thing>((await SnooStreamViewModel.RedditService.GetPostsBySubreddit(
                    Utility.CleanRedditLink(SnooStreamViewModel.Settings.LockScreenReddit, user.Username), "hot", 10)).Data.Children);
                Shuffle(frontPageResult);
                lockScreenMessages.AddRange(frontPageResult.Where(thing => thing.Data is Link)
                    .Take(SnooStreamViewModel.Settings.OverlayItemCount - lockScreenMessages.Count).Select(thing => new LockScreenMessage 
                        { DisplayText = ((Link)thing.Data).Title, Glyph = linkGlyphConverter != null ? (string)linkGlyphConverter.Convert(((Link)thing.Data), typeof(String), null, System.Globalization.CultureInfo.CurrentCulture) : "" }
                     )
                );
            }

            List<string> shuffledLockScreenImages = new List<string>(lockScreenImages);
            Shuffle(shuffledLockScreenImages);

            var vml = new SnooStreamWP8.BackgroundControls.ViewModel.LockScreenViewModel();
            vml.ImageSource = shuffledLockScreenImages.FirstOrDefault();
            vml.OverlayItems = lockScreenMessages;
            vml.OverlayOpacity = SnooStreamViewModel.Settings.OverlayOpacity;
            vml.NumberOfItems = SnooStreamViewModel.Settings.OverlayItemCount;
            vml.RoundedCorners = SnooStreamViewModel.Settings.RoundedLockScreen;
            return vml;
        }

        public static void Shuffle<T>(IList<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
