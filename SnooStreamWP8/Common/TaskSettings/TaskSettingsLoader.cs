using Procurios.Public;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.TaskSettings
{
	public class TaskSettingsLoader
	{
		public static TaskSettings LoadTaskSettings()
		{
			var result = new TaskSettings();

			if (File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "taskSettings.json"))
			{
				using (var cookieFile = File.OpenRead(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "taskSettings.json"))
				{
					byte[] taskCookieBytes = new byte[4096];
					int readBytes = cookieFile.Read(taskCookieBytes, 0, 4096);

					var json = Encoding.UTF8.GetString(taskCookieBytes, 0, readBytes);
					var decodedJson = JSON.JsonDecode(json);

					if (JSON.Contains(decodedJson, (int)SettingIdentifiers.RedditCookie))
					{
						result.RedditCookie = (string)JSON.GetValue(decodedJson, (int)SettingIdentifiers.RedditCookie);
					}

					if (JSON.Contains(decodedJson, (int)SettingIdentifiers.LockScreenOverlayRoundedEdges))
					{
						result.LockScreenOverlayRoundedEdges = (bool)JSON.GetValue(decodedJson, (int)SettingIdentifiers.LockScreenOverlayRoundedEdges);
					}

					if (JSON.Contains(decodedJson, (int)SettingIdentifiers.LockScreenOverlayOpacity))
					{
						var val = (double?)JSON.GetValue(decodedJson, (int)SettingIdentifiers.LockScreenOverlayOpacity);
						if (val.HasValue)
							result.LockScreenOverlayOpacity = (int)val.Value;
					}

					if (JSON.Contains(decodedJson, (int)SettingIdentifiers.LockScreenOverlayItemsReddit))
					{
						result.LockScreenOverlayItemsReddit = (string)JSON.GetValue(decodedJson, (int)SettingIdentifiers.LockScreenOverlayItemsReddit) ?? "/";
					}

					if (JSON.Contains(decodedJson, (int)SettingIdentifiers.LockScreenOverlayItemsCount))
					{
						var val = (double?)JSON.GetValue(decodedJson, (int)SettingIdentifiers.LockScreenOverlayItemsCount);
						if (val.HasValue)
							result.LockScreenOverlayItemsCount = (int)val.Value;
					}

					if (JSON.Contains(decodedJson, (int)SettingIdentifiers.LockScreenImageURIs))
					{
						var val = JSON.GetValue(decodedJson, (int)SettingIdentifiers.LockScreenImageURIs) as List<object>;
						result.LockScreenImageURIs = val.Cast<string>().ToList();
					}

					if (JSON.Contains(decodedJson, (int)SettingIdentifiers.LiveTileStyle))
					{
						var val = (double?)JSON.GetValue(decodedJson, (int)SettingIdentifiers.LiveTileStyle);
						if (val.HasValue)
							result.LiveTileStyle = (LiveTileStyle)(int)val.Value;
					}

					if (JSON.Contains(decodedJson, (int)SettingIdentifiers.LiveTileItemsReddit))
					{
						result.LiveTileItemsReddit = (string)JSON.GetValue(decodedJson, (int)SettingIdentifiers.LiveTileItemsReddit) ?? "/";
					}

					if (JSON.Contains(decodedJson, (int)SettingIdentifiers.LiveTileImageURIs))
					{
						var val = JSON.GetValue(decodedJson, (int)SettingIdentifiers.LiveTileImageURIs) as List<object>;
						result.LiveTileImageURIs = val.Cast<string>().ToList();
					}
				}
			}

			return result;
		}
	}
}
