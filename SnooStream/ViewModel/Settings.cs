using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using SnooStream.Common;
using SnooStream.ViewModel.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public interface ISettingsContext
    {
        Dictionary<string, string> Settings { get; }
        void SettingsChanged();
    }

    public class SettingsViewModel
    {
        private ISettingsContext _settingsContext;
        public SettingsViewModel(ISettingsContext settingsContext)
        {
            _settingsContext = settingsContext;
        }

        public bool LeftSideNav { get { return DefaultGet("LeftSideNav", true); } set { Set("LeftSideNav", value); } }
        public int ContentTimeout { get { return DefaultGet("ContentTimeout", 60 * 1000); } set { Set("ContentTimeout", value); } }
        public bool AllowOver18 { get { return DefaultGet("AllowOver18", false); } set { Set("AllowOver18", value); } }
        public bool AllowOver18Items { get { return DefaultGet("AllowOver18Items", false); } set { Set("AllowOver18Items", value); } }
        public bool OpenLinksInBrowser { get { return DefaultGet("OpenLinksInBrowser", false); } set { Set("OpenLinksInBrowser", value); } }
        public bool HighlightAlreadyClickedLinks { get { return DefaultGet("HighlightAlreadyClickedLinks", true); } set { Set("HighlightAlreadyClickedLinks", value); } }
        public bool ApplyReadabliltyToLinks { get { return DefaultGet("ApplyReadabliltyToLinks", true); } set { Set("ApplyReadabliltyToLinks", value); } }
        public bool LeftHandedMode { get { return DefaultGet("LeftHandedMode", false); } set { Set("LeftHandedMode", value); } }
        public bool HeavyPreview { get { return DefaultGet("HeavyPreview", true); } set { Set("HeavyPreview", value); } }
        public bool PromptForCaptcha { get { return DefaultGet("PromptForCaptcha", true); } set { Set("PromptForCaptcha", value); } }
        public bool EnableUpdates { get { return DefaultGet("EnableUpdates", true); } set { Set("EnableUpdates", value); } }
        public bool EnableOvernightUpdates { get { return DefaultGet("EnableOvernightUpdates", true); } set { Set("EnableOvernightUpdates", value); } }
        public bool UpdateOverlayOnlyOnWifi { get { return DefaultGet("UpdateOverlayOnlyOnWifi", false); } set { Set("UpdateOverlayOnlyOnWifi", value); } }
        public bool UpdateImagesOnlyOnWifi { get { return DefaultGet("UpdateImagesOnlyOnWifi", true); } set { Set("UpdateImagesOnlyOnWifi", value); } }
        public bool UseImagePickerForLockScreen { get { return DefaultGet("UseImagePickerForLockScreen", false); } set { Set("UseImagePickerForLockScreen", value); } }
        public bool MessagesInLockScreenOverlay { get { return DefaultGet("MessagesInLockScreenOverlay", true); } set { Set("MessagesInLockScreenOverlay", value); } }
        public bool PostsInLockScreenOverlay { get { return DefaultGet("PostsInLockScreenOverlay", true); } set { Set("PostsInLockScreenOverlay", value); } }
        public string ImagesSubreddit { get { return DefaultGet("ImagesSubreddit", "/r/earthporn+InfrastructurePorn+MachinePorn"); } set { Set("ImagesSubreddit", value); } }
        public int OverlayOpacity { get { return DefaultGet("OverlayOpacity", 35); } set { Set("OverlayOpacity", value); } }
        public int OverlayItemCount { get { return DefaultGet("OverlayItemCount", 5); } set { Set("OverlayItemCount", value); } }
        public string LockScreenReddit { get { return DefaultGet("LockScreenReddit", "/"); } set { Set("LockScreenReddit", value); } }
        public string LiveTileReddit { get { return DefaultGet("LiveTileReddit", "/"); } set { Set("LiveTileReddit", value); } }
        public int OfflineCacheDays { get { return DefaultGet("OfflineCacheDays", 2); } set { Set("OfflineCacheDays", value); } }
        public bool LockScreenOverlay { get { return DefaultGet("LockScreenOverlay", true); } set { Set("LockScreenOverlay", value); } }
        public bool RoundedLockScreen { get { return DefaultGet("RoundedLockScreen", true); } set { Set("RoundedLockScreen", value); } }
        public bool MultiColorCommentMargins { get { return DefaultGet("MultiColorCommentMargins", false); } set { Set("MultiColorCommentMargins", value); } }
        public bool InvertSystemTheme { get { return DefaultGet("InvertSystemTheme", false); } set { Set("InvertSystemTheme", value); } }
        public bool OnlyFlipViewUnread { get { return DefaultGet("OnlyFlipViewUnread", false); } set { Set("OnlyFlipViewUnread", value); } }
        public bool OnlyFlipViewImages { get { return DefaultGet("OnlyFlipViewImages", true); } set { Set("OnlyFlipViewImages", value); } }
        public bool AllowAdvertising { get { return DefaultGet("AllowAdvertising", true); } set { Set("AllowAdvertising", value); } }
        public bool DisableBackground { get { return DefaultGet("DisableBackground", false); } set { Set("DisableBackground", value); } }
        public DateTime LastUpdatedImages { get { return DefaultGet("LastUpdatedImages", new DateTime()); } set { Set("LastUpdatedImages", value); } }
        public DateTime LastCleanedCache { get { return DefaultGet("LastCleanedCache", new DateTime()); } set { Set("LastCleanedCache", value); } }

        internal string DefaultGet(string key, string defaultValue)
        {
            string result;
            if (!_settingsContext.Settings.TryGetValue(key, out result))
                return defaultValue;
            else
                return result;
        }

        internal void Set(string key, string newValue)
        {
            string result;
            if (!_settingsContext.Settings.TryGetValue(key, out result))
            {
                _settingsContext.Settings.Add(key, newValue);
                _settingsContext.SettingsChanged();
            }
            else if (result != newValue)
            {
                _settingsContext.Settings[key] = newValue;
                _settingsContext.SettingsChanged();
            }
        }

        internal bool DefaultGet(string key, bool defaultValue)
        {
            string result;
            if (!_settingsContext.Settings.TryGetValue(key, out result))
            {
                return defaultValue;
            }
            else
                return bool.Parse(result);
        }

        internal void Set(string key, bool newValue)
        {
            string result;
            if (!_settingsContext.Settings.TryGetValue(key, out result))
            {
                _settingsContext.Settings.Add(key, newValue.ToString());
                _settingsContext.SettingsChanged();
            }
            else if (result != newValue.ToString())
            {
                _settingsContext.Settings[key] = newValue.ToString();
                _settingsContext.SettingsChanged();
            }
        }

        internal int DefaultGet(string key, int defaultValue)
        {
            string result;
            if (!_settingsContext.Settings.TryGetValue(key, out result))
            {
                return defaultValue;
            }
            else
                return int.Parse(result);
        }

        internal void Set(string key, int newValue)
        {
            string result;
            if (!_settingsContext.Settings.TryGetValue(key, out result))
            {
                _settingsContext.Settings.Add(key, newValue.ToString());
                _settingsContext.SettingsChanged();
            }
            else if (result != newValue.ToString())
            {
                _settingsContext.Settings[key] = newValue.ToString();
                _settingsContext.SettingsChanged();
            }
        }

        internal double DefaultGet(string key, double defaultValue)
        {
            string result;
            if (!_settingsContext.Settings.TryGetValue(key, out result))
            {
                return defaultValue;
            }
            else
                return double.Parse(result);
        }

        internal void Set(string key, float newValue)
        {
            string result;
            if (!_settingsContext.Settings.TryGetValue(key, out result))
            {
                _settingsContext.Settings.Add(key, newValue.ToString());
                _settingsContext.SettingsChanged();
            }
            else if (result != newValue.ToString())
            {
                _settingsContext.Settings[key] = newValue.ToString();
                _settingsContext.SettingsChanged();
            }
        }

        internal DateTime DefaultGet(string key, DateTime defaultValue)
        {
            string result;
            if (!_settingsContext.Settings.TryGetValue(key, out result))
            {
                return defaultValue;
            }
            else
                return DateTime.Parse(result);
        }

        internal void Set(string key, DateTime newValue)
        {
            string result;
            if (!_settingsContext.Settings.TryGetValue(key, out result))
            {
                _settingsContext.Settings.Add(key, newValue.ToString());
                _settingsContext.SettingsChanged();
            }
            else if (result != newValue.ToString())
            {
                _settingsContext.Settings[key] = newValue.ToString();
                _settingsContext.SettingsChanged();
            }
        }
    }

    class SettingsContext : ISettingsContext
    {
        public Dictionary<string, string> Settings { get; set; }

        public void SettingsChanged()
        {
            Messenger.Default.Send<SettingsChangedMessage>(new SettingsChangedMessage());
        }
    }
}
