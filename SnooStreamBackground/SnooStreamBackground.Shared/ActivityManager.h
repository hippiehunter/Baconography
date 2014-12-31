#pragma once

#include <vector>

namespace SnooStreamBackground
{
    ref class SimpleRedditService;
    public ref class ActivityManager sealed
    {
    private:
       std::vector<Platform::String^> _alreadyToasted;
       chrono::seconds _lastUpdate;
       SimpleRedditService^ _redditService;

       void MakeToast(Platform::String^ id, Platform::String^ text, Windows::Foundation::TypedEventHandler<Windows::UI::Notifications::ToastNotification^, Object^>^ activatedHandler);
    public:
        ActivityManager(SimpleRedditService^ redditService);
        property Platform::String^ SentBlob;
        property Platform::String^ RecivedBlob;
        property Platform::String^ ActivityBlob;
        property bool NeedsRefresh
        {
            bool get();
        }
        property int UpdateCountSinceToast;
        Windows::Foundation::IAsyncAction^ Refresh(Windows::Foundation::TypedEventHandler<Windows::UI::Notifications::ToastNotification^, Object^>^ activatedHandler);
    };
}