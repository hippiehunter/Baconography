using CommonResourceAcquisition.ImageAcquisition;
using GalaSoft.MvvmLight.Messaging;
using SnooSharp;
using SnooStream.Model;
using SnooStream.TaskSettings;
using SnooStream.ViewModel;
using SnooStream.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using SnooStreamBackground;
using Windows.UI.Popups;
using Windows.ApplicationModel.Background;

namespace SnooStream.Common
{
    class BackgroundTaskManager
    {
        static bool _imageLoadInProgress = false;
        
#if WINDOWS_PHONE_APP

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
			var taskSettings = new LockScreenSettings();
            if (!_imageLoadInProgress && taskSettings.LockScreenImageURIs.Count <= 1)
                return await UpdateLockScreenImages();
            return false;
        }

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
				var taskSettings = new LockScreenSettings();
				taskSettings.LockScreenImageURIs.Clear();
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

                            imageSource.SetSource(stream.AsRandomAccessStream());
                        }

                        if (imageSource.PixelHeight == 0 || imageSource.PixelWidth == 0)
                            continue;

                        if (imageSource.PixelHeight < 800
                                || imageSource.PixelWidth < 480)
                            continue;

                        MakeSingleLockScreenFromImage(taskSettings.LockScreenImageURIs.Count, imageSource);
                        //this can happen when the user is still trying to use the application so dont lock up the UI thread with this work
                        await Task.Yield();
						taskSettings.LockScreenImageURIs.Add(url, string.Format("lockScreenCache{0}.jpg", taskSettings.LockScreenImageURIs.Count.ToString()));
						if (taskSettings.LockScreenImageURIs.Count >= limit)
                            break;
                    }
                    catch (OutOfMemoryException oom)
                    {
                        // Ouch
                    }
                }
                taskSettings.WriteSettings();

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
				var taskSettings = new LockScreenSettings();
                string imageUrl = "";
                // ms-appx points to the Local app install folder, to reference resources bundled in the XAP package.
                // ms-appdata points to the root of the local app data folder.
				if (taskSettings.LockScreenImageURIs.Count <= 0)
                {
                    imageUrl = "ms-appx:///Assets/RainbowGlass.jpg";
                }
                else
                {
					//TODO improve shuffle here
					imageUrl = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\" + taskSettings.LockScreenImageURIs.First().Value;
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

		public async Task<SnooStream.BackgroundControls.ViewModel.LockScreenViewModel> MakeLockScreenControl(IEnumerable<string> lockScreenImages)
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

            var vml = new SnooStream.BackgroundControls.ViewModel.LockScreenViewModel();
            vml.ImageSource = shuffledLockScreenImages.FirstOrDefault();
            vml.OverlayItems = lockScreenMessages;
            vml.OverlayOpacity = SnooStreamViewModel.Settings.OverlayOpacity;
            vml.NumberOfItems = SnooStreamViewModel.Settings.OverlayItemCount;
            vml.RoundedCorners = SnooStreamViewModel.Settings.RoundedLockScreen;
            return vml;
        }

#endif

		public static bool CanDownload
        {
            get
            {
				var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
				var connectionCostType = connectionProfile.GetConnectionCost().NetworkCostType;
				var connectionStrength = connectionProfile.GetSignalBars() ?? 5;
				if ((SnooStreamViewModel.Settings.UpdateImagesOnlyOnWifi && connectionProfile.IsWlanConnectionProfile)
					|| (!SnooStreamViewModel.Settings.UpdateImagesOnlyOnWifi && connectionCostType != NetworkCostType.Variable && connectionStrength > 1))
                    return true;

                return false;
            }
        }

        public static bool DownloadOptimal
        {
            get
            {
				var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
				var connectionCostType = connectionProfile.GetConnectionCost().NetworkCostType;
				var connectionStrength = connectionProfile.GetSignalBars() ?? 5;

				if ((SnooStreamViewModel.Settings.UpdateImagesOnlyOnWifi && connectionProfile.IsWlanConnectionProfile)
					|| (!SnooStreamViewModel.Settings.UpdateImagesOnlyOnWifi && connectionCostType == NetworkCostType.Unrestricted && connectionStrength > 3))
                    return true;

                return false;
            }
        }

        public static readonly string periodicTaskName = "SnooStream_LockScreen_Updater";

        public static void RemoveAgent(string name)
        {
            try
            {
				var periodicTask = BackgroundTaskRegistration.AllTasks.FirstOrDefault(task => task.Value.Name == name);
				// Obtain a reference to the period task, if one exists

				if (periodicTask.Value != null)
				{
					// If the task already exists and background agents are enabled for the
					// application, you must remove the task and then add it again to update 
					// the schedule
					periodicTask.Value.Unregister(false);
				}
            }
            catch (Exception)
            {
            }
        }

        public static async void StartPeriodicAgent()
        {
			// Place the call to Add in a try block in case the user has disabled agents.
			try
			{
				var periodicTask = BackgroundTaskRegistration.AllTasks.FirstOrDefault(task => task.Value.Name == periodicTaskName);
				// Obtain a reference to the period task, if one exists

				if (periodicTask.Value != null)
				{
					// If the task already exists and background agents are enabled for the
					// application, you must remove the task and then add it again to update 
					// the schedule
					periodicTask.Value.Unregister(false);
				}


				var disableBackground = SnooStreamViewModel.Settings.DisableBackground;
				if (!disableBackground)
				{
					var access = await BackgroundExecutionManager.RequestAccessAsync();
					if (access != BackgroundAccessStatus.Denied && access != BackgroundAccessStatus.Unspecified)
					{
						var builder = new BackgroundTaskBuilder();

						builder.Name = periodicTaskName;

						builder.TaskEntryPoint = "SnooStreamBackground.UpdateBackgroundTask";
						builder.SetTrigger(new TimeTrigger(30, false));
						builder.AddCondition(new SystemCondition(SystemConditionType.BackgroundWorkCostNotHigh));
						builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
						var taskRegistration = builder.Register();
					}
				}
			}
			catch (InvalidOperationException exception)
			{
				if (exception.Message.Contains("BNS Error: The action is disabled"))
				{
					var dialog = new MessageDialog("Background agents for this application have been disabled by the user.");
					var asyncOp = dialog.ShowAsync(); //just continue it doesnt matter at this point anyway
				}

				if (exception.Message.Contains("BNS Error: The maximum number of ScheduledActions of this type have already been added."))
				{
					// No user action required. The system prompts the user when the hard limit of periodic tasks has been reached.

				}
			}
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
