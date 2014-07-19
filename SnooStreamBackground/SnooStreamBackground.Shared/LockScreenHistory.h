#pragma once

#include <string>
#include "pch.h"

namespace SnooStreamBackground
{
  public ref class LockScreenImageInfo sealed
  {
  public:
    property Platform::String^ OriginalUrl;
    property Platform::String^ LocalUrl;
    property Platform::String^ LastShown;
    LockScreenImageInfo(Platform::String^ originalUrl, Platform::String^ localUrl, Platform::String^ lastShown)
    {
      OriginalUrl = originalUrl;
      LocalUrl = localUrl;
      LastShown = lastShown;
    }
  };

  public ref class LockScreenHistory sealed
  {
  private:
    Platform::Collections::UnorderedMap<Platform::String^, Platform::String^>^ _history;
    Platform::Collections::UnorderedMap<Platform::String^, Platform::String^>^ _toastedMessages;
  public:
    property Windows::Foundation::Collections::IVector<LockScreenImageInfo^>^ CurrentTileImages;
    property Windows::Foundation::Collections::IVector<LockScreenImageInfo^>^ LockScreenImages;
    property Platform::String^ LastLockScreenUpdate;


    bool HasHistory(Platform::String^ url);
    bool Toast(Platform::String^ messageId, Platform::String^ message);
    LockScreenHistory();
    void Store();
  };
}