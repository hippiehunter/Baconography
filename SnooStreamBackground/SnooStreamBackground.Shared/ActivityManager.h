#pragma once

#include <vector>
#include <chrono>

namespace SnooStreamBackground
{
    class SimpleRedditService;
    public ref class ActivityManager sealed
    {
    private:
       std::vector<Platform::String^> _alreadyToasted;
       std::chrono::seconds _lastUpdate;

       void MakeToast(Platform::String^ id, Platform::String^ text, Windows::Foundation::TypedEventHandler<Windows::UI::Notifications::ToastNotification^, Object^>^ activatedHandler);
        
    public:
		ActivityManager();
        property Platform::String^ SentBlob;
        property Platform::String^ ReceivedBlob;
        property Platform::String^ ActivityBlob;
        property bool NeedsRefresh
        {
            bool get();
        }
        property int UpdateCountSinceActivity;
        Windows::Foundation::IAsyncAction^ Refresh(Platform::String^ oAuthBlob, Windows::Foundation::TypedEventHandler<Windows::UI::Notifications::ToastNotification^, Object^>^ activatedHandler);
        void StoreState();
    };
}