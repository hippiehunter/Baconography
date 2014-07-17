#pragma once


namespace SnooStreamBackground
{
	public enum class LiveTileStyle
	{
		Off,
		Cycle,
		Default,
		Blergh
	};

	// DO NOT REUSE THESE VALUES,
	// THERE ARE PLENTY FOR EXPANSION
	public enum class SettingIdentifiers
	{
		RedditCookie = 0x001,

		LockScreenOverlayRoundedEdges = 0xA01,
		LockScreenOverlayOpacity = 0xA02,
		LockScreenOverlayItemsCount = 0xA03,
		LockScreenOverlayItemsReddit = 0xA04,

		LiveTileStyle = 0xB01,
		LiveTileItemsReddit = 0xB02,
	};

	public ref class LockScreenSettings sealed
	{
	public:
		//Lock screen settings
		property bool LockScreenOverlayRoundedEdges;
		property int LockScreenOverlayOpacity;
		property int LockScreenOverlayItemsCount;
		property Platform::String^ LockScreenOverlayItemsReddit;
		
		//Live Tile settings
		property LiveTileStyle LiveTileStyle;
		property Platform::String^ LiveTileItemsReddit;
		
		property Platform::String^ RedditCookie;

		LockScreenSettings();
		void WriteSettings();
	};
}