#pragma once

#include<ppltasks.h>
#include <vector>
#include <tuple>

#include "LockScreenHistory.h"

namespace SnooStreamBackground
{
    public ref class ImageUtilities sealed
    {
    public:

    internal:
        static concurrency::task<std::vector<ImageInfo^>> MakeLiveTileImages(std::vector<ImageInfo^> liveTileFiles, LockScreenHistory^ history, std::vector<std::tuple<Platform::String^, Platform::String^>> liveTileTpls, int targetCount, int targetIndex = 0);
        static concurrency::task<Nokia::Graphics::Imaging::IImageProvider^> GetImageSource(Platform::String^ url);
        static Platform::String^ ComputeMD5(Platform::String^ str);
        static concurrency::task<Platform::String^> MakeTileSizedImage(Nokia::Graphics::Imaging::IImageProvider^ imageSource, Platform::String^ url, float height, float width);
        static concurrency::task<void> ClearOldTempImages();
    };

    
}