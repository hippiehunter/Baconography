#pragma once

#include <unordered_set>

namespace SnooStreamBackground
{
    public ref class ActivityManager sealed
    {
    private:
        std::unordered_set<std::wstring> _alreadyToasted;
    public:
        ActivityManager();
        property Platform::String^ ActivityBlob
        {
            Platform::String^ get();
        }
        property bool NeedsRefresh
        {
            bool get();
        }
        Windows::Foundation::IAsyncAction^ Refresh(Windows::Foundation::TypedEventHandler<Windows::UI::Notifications::ToastNotification^, Object^>^ activatedHandler);
    };
}