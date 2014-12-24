#pragma once

#include <vector>

#include "LockScreenHistory.h"
#include "LockScreenSettings.h"

namespace SnooStreamBackground
{
    public ref class LiveTileUtilities sealed
    {
    internal:
        static void MakeLiveTile(LockScreenHistory^ history, LiveTileSettings^ liveTile, std::vector<ImageInfo^> options, Windows::UI::Notifications::TileUpdater^ updater);
    public:
        static void MakeLiveTile(LockScreenHistory^ history, LiveTileSettings^ liveTile, Windows::Foundation::Collections::IVector<ImageInfo^>^ options, Windows::UI::Notifications::TileUpdater^ updater);
    };
}