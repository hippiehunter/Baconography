using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.TaskSettings
{
	public static class TaskSettingsExtensions
	{
		public static void SaveTaskSettings(this TaskSettings settings)
		{
			lock (typeof(TaskSettings))
			{
				using (var taskCookieFile = File.Create(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "taskSettings.json"))
				{
					var textWriter = new StreamWriter(taskCookieFile);
					var jsonWriter = new JsonTextWriter(textWriter);
					jsonWriter.WriteStartObject();
					jsonWriter.WritePropertyName(((int)SettingIdentifiers.RedditCookie).ToString());
					jsonWriter.WriteValue(settings.RedditCookie);
					jsonWriter.WritePropertyName(((int)SettingIdentifiers.LockScreenOverlayRoundedEdges).ToString());
					jsonWriter.WriteValue(settings.LockScreenOverlayRoundedEdges);
					jsonWriter.WritePropertyName(((int)SettingIdentifiers.LockScreenOverlayOpacity).ToString());
					jsonWriter.WriteValue(settings.LockScreenOverlayOpacity);
					jsonWriter.WritePropertyName(((int)SettingIdentifiers.LockScreenOverlayItemsReddit).ToString());
					jsonWriter.WriteValue(settings.LockScreenOverlayItemsReddit);
					jsonWriter.WritePropertyName(((int)SettingIdentifiers.LockScreenOverlayItemsCount).ToString());
					jsonWriter.WriteValue(settings.LockScreenOverlayItemsCount);
					jsonWriter.WritePropertyName(((int)SettingIdentifiers.LockScreenImageURIs).ToString());
					jsonWriter.WriteStartArray();
					foreach (var image in settings.LockScreenImageURIs)
						jsonWriter.WriteValue(image);
					jsonWriter.WriteEndArray();
					jsonWriter.WritePropertyName(((int)SettingIdentifiers.LiveTileStyle).ToString());
					jsonWriter.WriteValue((int)settings.LiveTileStyle);
					jsonWriter.WritePropertyName(((int)SettingIdentifiers.LiveTileItemsReddit).ToString());
					jsonWriter.WriteValue(settings.LiveTileItemsReddit);
					jsonWriter.WritePropertyName(((int)SettingIdentifiers.LiveTileImageURIs).ToString());
					jsonWriter.WriteStartArray();
					foreach (var image in settings.LiveTileImageURIs)
						jsonWriter.WriteValue(image);
					jsonWriter.WriteEndArray();
					jsonWriter.WriteEndObject();
					jsonWriter.Flush();
				}
			}
		}
	}
}
