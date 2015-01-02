#include "pch.h"
#include "ActivityManager.h"
#include "SimpleRedditService.h"

#include <fstream>
#include <string>
#include <sstream>
#include <unordered_set>

using namespace std;
using namespace SnooStreamBackground;
using namespace Windows::Data::Json;
using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::UI::Notifications;

ActivityManager::ActivityManager()
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
        ReceivedBlob = parsedFileObject->GetNamedString("received");
        SentBlob = parsedFileObject->GetNamedString("sent");
        ActivityBlob = parsedFileObject->GetNamedString("activity");
        UpdateCountSinceActivity = (int)parsedFileObject->GetNamedNumber("updateSinceActivity");
        _lastUpdate = chrono::seconds((long long)parsedFileObject->GetNamedNumber("lastUpdate"));
        for (auto toasted : toastedArray)
        {
            _alreadyToasted.push_back(toasted->GetString());
        }
    }
}

void ActivityManager::StoreState()
{
    auto serializedObject = ref new JsonObject();
    auto toastHistory = ref new JsonArray();
    for (auto toast : _alreadyToasted)
    {
        if (toastHistory->Size < 100)
            toastHistory->Append(JsonValue::CreateStringValue(toast));
    }

    serializedObject->SetNamedValue("toasted", toastHistory);
    serializedObject->SetNamedValue("received", JsonValue::CreateStringValue(ReceivedBlob));
    serializedObject->SetNamedValue("sent", JsonValue::CreateStringValue(SentBlob));
    serializedObject->SetNamedValue("activity", JsonValue::CreateStringValue(ActivityBlob));
    serializedObject->SetNamedValue("updateSinceActivity", JsonValue::CreateNumberValue(UpdateCountSinceActivity));
    serializedObject->SetNamedValue("lastUpdate", JsonValue::CreateNumberValue(_lastUpdate.count()));

    auto serializedString = serializedObject->Stringify();
    wstring localPath(Windows::Storage::ApplicationData::Current->LocalFolder->Path->Data());
    localPath += L"\\bgtaskActivity.txt";
    wofstream activityFile(localPath, std::ios_base::out | std::ios_base::binary | std::ios_base::trunc, _SH_DENYRW);
    wstring activityFileString(serializedString->Data(), serializedString->Length());
    activityFile << activityFileString;
    activityFile.close();
}

//grab new data if its been at least 60 minutes since the last time we checked or 5 minutes (increasing by 5 minutes each refresh without a toast)
//if our last update pulled a toast notification
bool ActivityManager::NeedsRefresh::get()
{
    auto updatePeriod = chrono::duration_cast<chrono::seconds>(chrono::system_clock::now().time_since_epoch()) - _lastUpdate;
	if (_lastUpdate.count() == 0)
		return true;
    if (chrono::duration_cast<chrono::minutes>(updatePeriod).count() > 60)
        return true;
    else if (chrono::duration_cast<chrono::minutes>(updatePeriod).count() > ((UpdateCountSinceActivity * 5) + 5))
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

IAsyncAction^ ActivityManager::Refresh(Platform::String^ oAuthBlob, TypedEventHandler<ToastNotification^, Object^>^ activatedHandler)
{
    std::shared_ptr<SimpleRedditService> redditService = make_shared<SimpleRedditService>(RedditOAuth::Deserialize(oAuthBlob));
    UpdateCountSinceActivity++;
    //grab new data
    return concurrency::create_async([=]()
    {
        return redditService->GetActivity()
            .then([=](concurrency::task<Activities> result)
        {
            try
            {
                auto resultActivities = result.get();
                ActivityBlob = resultActivities.Blob;
                return redditService->GetSent();
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

                return redditService->GetMessages();
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
                std::unordered_set<Platform::String^> toastedLookup(_alreadyToasted.begin(), _alreadyToasted.end());
                auto resultActivities = result.get();
                ReceivedBlob = resultActivities.Blob;
                for (auto toastTpl : resultActivities.Toastables)
                {
                    if (toastedLookup.find(toastTpl->Key) == toastedLookup.end())
                    {
                        UpdateCountSinceActivity = 0;
                        MakeToast(toastTpl->Key, toastTpl->Value, activatedHandler);
                        toastedLookup.insert(toastTpl->Key);
                        _alreadyToasted.insert(_alreadyToasted.begin(), toastTpl->Key);
                    }
                }
				_lastUpdate = chrono::duration_cast<chrono::seconds>(chrono::system_clock::now().time_since_epoch());
                StoreState();
            }
            catch (...)
            {
            }
        });
    });
}


