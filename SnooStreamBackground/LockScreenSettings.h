#pragma once


namespace SnooStreamBackground
{
    public enum class LiveTileStyle
    {
        Off,
        ImageSet,
        Image,
        Text,
        TextImage
    };

    public enum class LockScreenStyle
    {
        Off,
        Image,
        Cycle
    };

    public ref class LiveTileSettings sealed
    {
    public:
        property LiveTileStyle LiveTileStyle;
        property Platform::String^ LiveTileItemsReddit;
        property Windows::Foundation::Collections::IVector<Platform::String^>^ CurrentImages;
    };

    public ref class LockScreenSettings sealed
    {
    public:
        //Lock screen settings
        property bool LockScreenOverlayRoundedEdges;
        property int LockScreenOverlayOpacity;
        property int LockScreenOverlayItemsCount;
        property Platform::String^ LockScreenOverlayItemsReddit;
        property LockScreenStyle LockScreenStyle;
        //Live Tile settings
        property Windows::Foundation::Collections::IVector<LiveTileSettings^>^ LiveTileSettings;

        property Platform::String^ RedditOAuth;

        LockScreenSettings();
        void Store();
    };
}