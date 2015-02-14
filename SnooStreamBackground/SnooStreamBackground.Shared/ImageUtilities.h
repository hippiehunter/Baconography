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
		static bool FileExists(Platform::String^ fileName);
        static Windows::Foundation::IAsyncOperation<Platform::String^>^ MakeSizedImage(Platform::String^ onDiskPrefix, Platform::String^ url, float height, float width);
        static Platform::String^ TrySizedImage(Platform::String^ onDiskPrefix, Platform::String^ url, float height, float width);
    internal:
        static concurrency::task<std::vector<ImageInfo^>> MakeLiveTileImages(std::vector<ImageInfo^> liveTileFiles, LockScreenHistory^ history, std::vector<std::tuple<Platform::String^, Platform::String^>> liveTileTpls, int targetCount, int targetIndex = 0);
        static concurrency::task<Lumia::Imaging::BufferImageSource^> GetImageSource(Platform::String^ url);
        static Platform::String^ ComputeMD5(Platform::String^ str);
        static Platform::String^ MakeTempFileName(Platform::String^ prefix, Platform::String^ url, int height, int width);
        static concurrency::task<Platform::String^> MakeTileSizedImage(Lumia::Imaging::BufferImageSource^ imageSource, Platform::String^ url, float height, float width, Windows::Storage::StorageFolder^ targetFolder);
        static concurrency::task<void> ClearOldTempImages();
    };

    
}