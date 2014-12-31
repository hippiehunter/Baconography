#include "pch.h"
#include "ActivityManager.h"
#include "SimpleRedditService.h"

#include <fstream>
#include <string>
#include <sstream>
#include <chrono>
#include <unordered_set>

using namespace std;
using namespace SnooStreamBackground;
using namespace Windows::Data::Json;
using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::UI::Notifications;

ActivityManager::ActivityManager(SimpleRedditService^ redditService)
{
    //grab activity blob from disk
    wstring activityPath(Windows::Storage::ApplicationData::Current->LocalFolder->Path->Data());
    activityPath += L"\\bgtaskActivity.txt";


    wifstream activityFile(activityPath, std::ios_base::in | std::ios_base::binary, _SH_DENYRW);
    wstringstream fileContents;
    wstring fileLine;

    while (activityFile.is_open() && !activityFile.eof())
    {
        activityFile >> fileLine;
        fileContents << fileLine;
    }
    activityFile.close();

    auto fileStr = fileContents.str();

    if (fileStr.size() > 0)
    {
        auto parsedFileObject = JsonObject::Parse(ref new String(fileStr.data(), fileStr.size()));
        auto toastedArray = parsedFileObject->GetNamedArray("toasted");
        RecivedBlob = parsedFileObject->GetNamedString("recived");
        SentBlob = parsedFileObject->GetNamedString("sent");
        ActivityBlob = parsedFileObject->GetNamedString("activity");
        UpdateCountSinceToast = (int)parsedFileObject->GetNamedNumber("updateSinceToast");
        _lastUpdate = chrono::seconds((long long)parsedFileObject->GetNamedNumber("lastUpdate"));
    }
}

//grab new data if its been at least 60 minutes since the last time we checked or 5 minutes (increasing by 5 minutes each refresh without a toast)
//if our last update pulled a toast notification
bool ActivityManager::NeedsRefresh::get()
{
    auto updatePeriod = _lastUpdate - chrono::duration_cast<chrono::seconds>(chrono::system_clock::now().time_since_epoch());
    if (chrono::duration_cast<chrono::minutes>(updatePeriod).count() > 60)
        return true;
    else if (chrono::duration_cast<chrono::minutes>(updatePeriod).count() > ((UpdateCountSinceToast * 5) + 5))
        return true;
    else
        return false;
}

void ActivityManager::MakeToast(String^ id, String^ text, TypedEventHandler<ToastNotification^, Object^>^ activatedHandler)
{
    auto notifier = ToastNotificationManager::CreateToastNotifier();
    auto toastXml = ToastNotificationManager::GetTemplateContent(ToastTemplateType::ToastText01);
    auto toastTextElements = toastXml->GetElementsByTagName("text");
    toastTextElements->Item(0)->InnerText = text;
    auto toastNode = toastXml->SelectSingleNode("/toast");
    static_cast<Windows::Data::Xml::Dom::XmlElement^>(toastNode)->SetAttribute("launch", "?activityid=" + id);
    auto notification = ref new ToastNotification(toastXml);
    if (activatedHandler != nullptr)
        notification->Activated += activatedHandler;

#if WINDOWS_PHONE
    notification->Tag = id;
#endif
    notifier->Show(notification);
}

IAsyncAction^ ActivityManager::Refresh(TypedEventHandler<ToastNotification^, Object^>^ activatedHandler)
{
    UpdateCountSinceToast++;
    //grab new data
    concurrency::create_async(_redditService->GetActivity()
        .then([=](concurrency::task<Activities> result)
    {
        try 
        {
            auto resultActivities = result.get();
            ActivityBlob = resultActivities.Blob;
            return _redditService->GetSent();
        }
        catch (...)
        {
            return result;
        }
        
    })
        .then([=](concurrency::task<Activities> result)
    {
        try
        {
            auto resultActivities = result.get();
            SentBlob = resultActivities.Blob;

            return _redditService->GetMessages();
        }
        catch (...)
        {
            return result;
        }
    }))
        .then([=](concurrency::task<Activities> result)
    {
        try
        {
            std::unordered_set<Platform::String^> toastedLookup(_alreadyToasted.begin(), _alreadyToasted.end());
            auto resultActivities = result.get();
            RecivedBlob = resultActivities.Blob;
            for (auto toastTpl : resultActivities.Toastables)
            {
                if (toastedLookup.find(toastTpl->Key) != toastedLookup.end())
                {
                    UpdateCountSinceToast = 0;
                    MakeToast(toastTpl->Key, toastTpl->Value, activatedHandler);
                    toastedLookup.insert(toastTpl->Key);
                    _alreadyToasted.insert(_alreadyToasted.begin(), toastTpl->Key);
                }
            }

            StoreState();
        }
        catch (...)
        {            
        }
    });
}
