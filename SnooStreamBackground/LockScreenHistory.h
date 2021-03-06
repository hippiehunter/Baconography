#pragma once

#include <string>
#include "pch.h"

namespace SnooStreamBackground
{
    public ref class ImageInfo sealed
    {
    public:
        property Platform::String^ OriginalUrl;
        property Platform::String^ LocalSmallSquareUrl;
        property Platform::String^ LocalLargeSquareUrl;
        property Platform::String^ LocalWideUrl;
        property int LastShown;
        property Platform::String^ Title;
		property bool Faulted;
        ImageInfo(Platform::String^ originalUrl, Platform::String^ localSmallSquareUrl, Platform::String^ localLargeSquareUrl, Platform::String^ localWideUrl, int lastShown, Platform::String^ title)
        {
            OriginalUrl = originalUrl;

            LocalSmallSquareUrl = localSmallSquareUrl;
            LocalLargeSquareUrl = localLargeSquareUrl;
            LocalWideUrl = localWideUrl;

            LastShown = lastShown;
            Title = title;
			Faulted = false;
        }
    };

    public ref class LockScreenHistory sealed
    {
    private:
        Platform::Collections::UnorderedMap<Platform::String^, int>^ _history;
    public:
        property Windows::Foundation::Collections::IVector<ImageInfo^>^ CurrentTileImages;
        property Windows::Foundation::Collections::IVector<ImageInfo^>^ LockScreenImages;
        property Platform::String^ LastLockScreenUpdate;


        int Age(Platform::String^ url);
        LockScreenHistory();
        void Store();
    };
}