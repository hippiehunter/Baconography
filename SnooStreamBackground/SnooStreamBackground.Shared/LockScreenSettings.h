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
		LockScreenImageURIs = 0xA06,

		LiveTileStyle = 0xB01,
		LiveTileItemsReddit = 0xB02,
		LiveTileImageURIs = 0xB04
	};

	public ref class LockScreenSettings
	{
	public:
		//Lock screen settings
		property bool LockScreenOverlayRoundedEdges;
		property int LockScreenOverlayOpacity;
		property int LockScreenOverlayItemsCount;
		property Platform::String^ LockScreenOverlayItemsReddit;
		property Windows::Foundation::Collections::IMap<Platform::String^, Platform::String^>^ LockScreenImageURIs;
		
		//Live Tile settings
		property LiveTileStyle LiveTileStyle;
		property Platform::String^ LiveTileItemsReddit;
		property Windows::Foundation::Collections::IMap<Platform::String^, Platform::String^>^ LiveTileImageURIs;
		
		property Platform::String^ RedditCookie;


		LockScreenSettings();
		void WriteSettings();
	};
}