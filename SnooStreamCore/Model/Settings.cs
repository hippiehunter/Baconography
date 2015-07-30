using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Model
{
    static class SettingsHelper
    {
        internal static string DefaultGet(this Dictionary<string, string> dict, string key, string defaultValue)
        {
            string result;
            if (!dict.TryGetValue(key, out result))
                return defaultValue;
            else
                return result;
        }

        internal static bool DefaultGet(this Dictionary<string, string> dict, string key, bool defaultValue)
        {
            string result;
            if (!dict.TryGetValue(key, out result))
            {
                return defaultValue;
            }
            else
                return bool.Parse(result);
        }

        internal static int DefaultGet(this Dictionary<string, string> dict, string key, int defaultValue)
        {
            string result;
            if (!dict.TryGetValue(key, out result))
            {
                return defaultValue;
            }
            else
                return int.Parse(result);
        }

		internal static double DefaultGet(this Dictionary<string, string> dict, string key, double defaultValue)
		{
			string result;
			if (!dict.TryGetValue(key, out result))
			{
				return defaultValue;
			}
			else
				return double.Parse(result);
		}

        internal static DateTime DefaultGet(this Dictionary<string, string> dict, string key, DateTime defaultValue)
        {
            string result;
            if (!dict.TryGetValue(key, out result))
            {
                return defaultValue;
            }
            else
                return DateTime.Parse(result);
        }
    }

    public class Settings
    {
        public Settings(Dictionary<string, string> init)
        {
			ContentTimeout = init.DefaultGet("ContentTimeout", 60 * 1000); //default to 60 seconds
            AllowOver18 = init.DefaultGet("AllowOver18", false);
            AllowOver18Items = init.DefaultGet("AllowOver18Items", false);
            OpenLinksInBrowser = init.DefaultGet("OpenLinksInBrowser", true);
            HighlightAlreadyClickedLinks = init.DefaultGet("HighlightAlreadyClickedLinks", true);
            ApplyReadabliltyToLinks = init.DefaultGet("ApplyReadabliltyToLinks", false);
            LeftHandedMode = init.DefaultGet("LeftHandedMode", false);
            OrientationLock = init.DefaultGet("OrientationLock", false);
            Orientation = init.DefaultGet("Orientation", "");
            AllowPrefetchOnMeteredConnection = init.DefaultGet("AllowPrefetchOnMeteredConnection", false);
            AllowPrefetch = init.DefaultGet("AllowPrefetch", true);
            PromptForCaptcha = init.DefaultGet("PromptForCaptcha", true);
            EnableUpdates = init.DefaultGet("EnableUpdates", true);
            EnableOvernightUpdates = init.DefaultGet("EnableOvernightUpdates", true);
            UpdateOverlayOnlyOnWifi = init.DefaultGet("UpdateOverlayOnlyOnWifi", false);
            UpdateImagesOnlyOnWifi = init.DefaultGet("UpdateImagesOnlyOnWifi", false);
            UseImagePickerForLockScreen = init.DefaultGet("UseImagePickerForLockScreen", false);
            MessagesInLockScreenOverlay = init.DefaultGet("MessagesInLockScreenOverlay", true);
            PostsInLockScreenOverlay = init.DefaultGet("PostsInLockScreenOverlay", true);
            ImagesSubreddit = init.DefaultGet("ImagesSubreddit", "/r/earthporn+InfrastructurePorn+MachinePorn");
            LockScreenOverlay = init.DefaultGet("LockScreenOverlay", true);
            OverlayOpacity = init.DefaultGet("OverlayOpacity", 35);
            OverlayItemCount = init.DefaultGet("OverlayItemCount", 5);
            LockScreenReddit = init.DefaultGet("LockScreenReddit", "/");
            LiveTileReddit = init.DefaultGet("LiveTileReddit", "/");
            OfflineCacheDays = init.DefaultGet("OfflineCacheDays", 2);
            TapForComments = init.DefaultGet("TapForComments", false);
            RoundedLockScreen = init.DefaultGet("RoundedLockScreen", false);
            MultiColorCommentMargins = init.DefaultGet("MultiColorCommentMargins", false);
            InvertSystemTheme = init.DefaultGet("InvertSystemTheme", false);
            OnlyFlipViewUnread = init.DefaultGet("OnlyFlipViewUnread", false);
            OnlyFlipViewImages = init.DefaultGet("OnlyFlipViewImages", true);
            AllowAdvertising = init.DefaultGet("AllowAdvertising", true);
            DisableBackground = init.DefaultGet("DisableBackground", false);
            ScreenWidth = init.DefaultGet("ScreenWidth", 480.0);
			ScreenHeight = init.DefaultGet("ScreenHeight", 800.0);
            LastUpdatedImages = init.DefaultGet("LastUpdatedImages", new DateTime());
            LastCleanedCache = init.DefaultGet("LastCleanedCache", new DateTime());
            HeavyPreview = init.DefaultGet("HeavyPreview", false);
            LeftSideNav = init.DefaultGet("LeftSideNav", true);
        }

        public bool LeftSideNav { get; set; }
        public int ContentTimeout { get; set; }
        public bool AllowOver18 { get; set; }
        public bool AllowOver18Items { get; set; }
        public bool OpenLinksInBrowser { get; set; }
        public bool HighlightAlreadyClickedLinks { get; set; }
        public bool ApplyReadabliltyToLinks { get; set; }
        public bool LeftHandedMode { get; set; }
        public bool OrientationLock { get; set; }
        public string Orientation { get; set; }
        public bool AllowPrefetchOnMeteredConnection { get; set; }
        public bool AllowPrefetch { get; set; }
        public bool HeavyPreview { get; set; }
        public bool PromptForCaptcha { get; set; }
        public bool EnableUpdates { get; set; }
        public bool EnableOvernightUpdates { get; set; }
        public bool UpdateOverlayOnlyOnWifi { get; set; }
        public bool UpdateImagesOnlyOnWifi { get; set; }
        public bool UseImagePickerForLockScreen { get; set; }
        public bool MessagesInLockScreenOverlay { get; set; }
        public bool PostsInLockScreenOverlay { get; set; }
        public string ImagesSubreddit { get; set; }
        public int OverlayOpacity { get; set; }
        public int OverlayItemCount { get; set; }
        public string LockScreenReddit { get; set; }
        public string LiveTileReddit { get; set; }
        public int OfflineCacheDays { get; set; }
        public bool TapForComments { get; set; }
        public bool LockScreenOverlay { get; set; }
        public bool RoundedLockScreen { get; set; }
        public bool MultiColorCommentMargins { get; set; }
        public bool InvertSystemTheme { get; set; }
        public bool OnlyFlipViewUnread { get; set; }
        public bool OnlyFlipViewImages { get; set; }
        public bool AllowAdvertising { get; set; }
        public bool DisableBackground { get; set; }
        public double ScreenWidth { get; set; }
        public double ScreenHeight { get; set; }
        public DateTime LastUpdatedImages { get; set; }
        public DateTime LastCleanedCache { get; set; }

        public Dictionary<string, string> Dump()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

			result.Add("ContentTimeout", ContentTimeout.ToString());
            result.Add("AllowOver18", AllowOver18.ToString());
            result.Add("AllowOver18Items", AllowOver18Items.ToString());
            result.Add("OpenLinksInBrowser", OpenLinksInBrowser.ToString());
            result.Add("HighlightAlreadyClickedLinks", HighlightAlreadyClickedLinks.ToString());
            result.Add("ApplyReadabliltyToLinks", ApplyReadabliltyToLinks.ToString());
            result.Add("LeftHandedMode", LeftHandedMode.ToString());
            result.Add("OrientationLock", OrientationLock.ToString());
            result.Add("Orientation", Orientation.ToString());
            result.Add("AllowPrefetchOnMeteredConnection", AllowPrefetchOnMeteredConnection.ToString());
            result.Add("AllowPrefetch", AllowPrefetch.ToString());
            result.Add("PromptForCaptcha", PromptForCaptcha.ToString());
            result.Add("EnableUpdates", EnableUpdates.ToString());
            result.Add("EnableOvernightUpdates", EnableOvernightUpdates.ToString());
            result.Add("UpdateOverlayOnlyOnWifi", UpdateOverlayOnlyOnWifi.ToString());
            result.Add("UpdateImagesOnlyOnWifi", UpdateImagesOnlyOnWifi.ToString());
            result.Add("UseImagePickerForLockScreen", UseImagePickerForLockScreen.ToString());
            result.Add("MessagesInLockScreenOverlay", MessagesInLockScreenOverlay.ToString());
            result.Add("PostsInLockScreenOverlay", PostsInLockScreenOverlay.ToString());
            result.Add("ImagesSubreddit", ImagesSubreddit.ToString());
            result.Add("LockScreenOverlay", LockScreenOverlay.ToString());
            result.Add("OverlayOpacity", OverlayOpacity.ToString());
            result.Add("OverlayItemCount", OverlayItemCount.ToString());
            result.Add("LockScreenReddit", LockScreenReddit.ToString());
            result.Add("LiveTileReddit", LiveTileReddit.ToString());
            result.Add("OfflineCacheDays", OfflineCacheDays.ToString());
            result.Add("TapForComments", TapForComments.ToString());
            result.Add("RoundedLockScreen", RoundedLockScreen.ToString());
            result.Add("MultiColorCommentMargins", MultiColorCommentMargins.ToString());
            result.Add("InvertSystemTheme", InvertSystemTheme.ToString());
            result.Add("OnlyFlipViewUnread", OnlyFlipViewUnread.ToString());
            result.Add("OnlyFlipViewImages", OnlyFlipViewImages.ToString());
            result.Add("AllowAdvertising", AllowAdvertising.ToString());
            result.Add("DisableBackground", DisableBackground.ToString()); 
            result.Add("ScreenWidth", ScreenWidth.ToString());
            result.Add("ScreenHeight", ScreenHeight.ToString());
            result.Add("LastUpdatedImages", LastUpdatedImages.ToString());
            result.Add("LastCleanedCache", LastCleanedCache.ToString());
            result.Add("HeavyPreview", HeavyPreview.ToString());
            result.Add("LeftSideNav", LeftSideNav.ToString());
            return result;

        }
    }
}
