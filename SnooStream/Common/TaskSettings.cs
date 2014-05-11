using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.TaskSettings
{
	public enum LiveTileStyle
	{
		Off,
		Cycle,
		Default,
		Blergh
	}

	// DO NOT REUSE THESE VALUES,
	// THERE ARE PLENTY FOR EXPANSION
	public enum SettingIdentifiers
	{
		RedditCookie = 0x001,

		LockScreenOverlayRoundedEdges = 0xA01,
		LockScreenOverlayOpacity = 0xA02,
		LockScreenOverlayItemsCount = 0xA03,
		LockScreenOverlayItemsReddit = 0xA04,
		LockScreenImageURIs = 0xA05,

		LiveTileStyle = 0xB01,
		LiveTileItemsReddit = 0xB02,
		LiveTileImageURIs = 0xB03
	}

	// Serializer for this class is located in WP8Core Utility.cs
	public class TaskSettings
	{
		#region Lock Screen Settings

		public bool LockScreenOverlayRoundedEdges
		{
			get;
			set;
		}
		public int LockScreenOverlayOpacity
		{
			get;
			set;
		}
		public int LockScreenOverlayItemsCount
		{
			get;
			set;
		}
		public string LockScreenOverlayItemsReddit
		{
			get;
			set;
		}
		public List<string> LockScreenImageURIs
		{
			get;
			set;
		}

		#endregion

		#region Live Tile Settings

		public LiveTileStyle LiveTileStyle
		{
			get;
			set;
		}
		public string LiveTileItemsReddit
		{
			get;
			set;
		}
		public List<string> LiveTileImageURIs
		{
			get;
			set;
		}

		#endregion

		public string RedditCookie
		{
			get;
			set;
		}

		public TaskSettings()
		{
			this.RedditCookie = "";
			this.LockScreenOverlayRoundedEdges = false;
			this.LockScreenOverlayOpacity = 35;
			this.LockScreenOverlayItemsReddit = "/";
			this.LockScreenOverlayItemsCount = 6;
			this.LockScreenImageURIs = new List<string>();
			this.LiveTileStyle = LiveTileStyle.Default;
			this.LiveTileItemsReddit = "/";
			this.LiveTileImageURIs = new List<string>();
		}
	}
}
