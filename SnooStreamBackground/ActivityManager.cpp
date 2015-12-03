#include "pch.h"
#include "ActivityManager.h"
#include "SimpleRedditService.h"
#include "FileUtilities.h"

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

	auto fileStr = readFileWithLock(activityPath);

    if (fileStr->Length() > 0)
    {
        JsonObject^ parsedFileObject = nullptr;
        if (JsonObject::TryParse(fileStr, &parsedFileObject))
        {
            auto toastedArray = parsedFileObject->GetNamedArray("toasted");
            ReceivedBlob = parsedFileObject->GetNamedString("received");
            SentBlob = parsedFileObject->GetNamedString("sent");
            ActivityBlob = parsedFileObject->GetNamedString("activity");
            UpdateCountSinceActivity = (int) parsedFileObject->GetNamedNumber("updateSinceActivity");
            _lastUpdate = chrono::seconds((long long) parsedFileObject->GetNamedNumber("lastUpdate"));
            for (auto toasted : toastedArray)
            {
                _alreadyToasted.push_back(toasted->GetString());
            }
        }
    }
}

void ActivityManager::ClearState()
{
    wstring localPath(Windows::Storage::ApplicationData::Current->LocalFolder->Path->Data());
    localPath += L"\\bgtaskActivity.txt";
    writeFileWithLock(L"", localPath, true);
}

void ActivityManager::StoreState()
{
    if (_canStore)
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
        writeFileWithLock(serializedString, localPath, true);
    }
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

Platform::String^ ActivityManager::ContextForId(Platform::String^ id)
{
    wstring localPath((Windows::Storage::ApplicationData::Current->TemporaryFolder->Path + L"\\activity_context" + id)->Data());
    wifstream contextFile(localPath, std::ios_base::binary | std::ios_base::in, _SH_DENYRW);
    if (contextFile.is_open())
    {
        auto contextStr = wstring(istreambuf_iterator<wchar_t>(contextFile), istreambuf_iterator<wchar_t>());
        contextFile.close();
        return ref new String(contextStr.c_str(), contextStr.length());
    }
    else
        return L"";
}

void ActivityManager::MakeToast(String^ id, String^ text, String^ context, TypedEventHandler<ToastNotification^, Object^>^ activatedHandler)
{
    auto notifier = ToastNotificationManager::CreateToastNotifier();
    auto toastXml = ToastNotificationManager::GetTemplateContent(ToastTemplateType::ToastText01);
    auto toastTextElements = toastXml->GetElementsByTagName("text");
    toastTextElements->Item(0)->InnerText = text;
    auto toastNode = toastXml->SelectSingleNode("/toast");

    if (context != nullptr)
    {
        wstring localPath((Windows::Storage::ApplicationData::Current->TemporaryFolder->Path + L"\\activity_context" + id)->Data());
        writeFileWithLock(context, localPath, true);
    }
    static_cast<Windows::Data::Xml::Dom::XmlElement^>(toastNode)->SetAttribute("launch", "{'activityid':'" + id + "'}");
    auto notification = ref new ToastNotification(toastXml);
    if (activatedHandler != nullptr)
        notification->Activated += activatedHandler;

#if WINDOWS_PHONE
    notification->Tag = id;
#endif
    notifier->Show(notification);
}

IAsyncActionWithProgress<float>^ ActivityManager::Refresh(Platform::String^ oAuthBlob, TypedEventHandler<ToastNotification^, Object^>^ activatedHandler, bool canStore)
{
    _canStore = canStore;
    std::shared_ptr<SimpleRedditService> redditService = make_shared<SimpleRedditService>(RedditOAuth::Deserialize(oAuthBlob));
    UpdateCountSinceActivity = UpdateCountSinceActivity + 1;
    //grab new data
    return concurrency::create_async([=](concurrency::progress_reporter<float> progress, concurrency::cancellation_token token)
    {
        return redditService->GetActivity()
            .then([=](concurrency::task<Activities> result)
        {
            progress.report(0.33f);
            try
            {
                auto resultActivities = result.get();
				if (!resultActivities.Faulted)
				{
					JsonObject^ validationResult = nullptr;
					if (JsonObject::TryParse(resultActivities.Blob, &validationResult))
					{
						ActivityBlob = resultActivities.Blob;
						return redditService->GetSent();
					}
				}
            }
			catch (...) {}
			return concurrency::task_from_result(Activities::MakeFaulted());

        }, token)
            .then([=](concurrency::task<Activities> result)
        {
            progress.report(0.66f);
            try
            {
                auto resultActivities = result.get();
				if (!resultActivities.Faulted)
				{
					JsonObject^ validationResult = nullptr;
					if (JsonObject::TryParse(resultActivities.Blob, &validationResult))
					{
						SentBlob = resultActivities.Blob;

						return redditService->GetMessages();
					}
				}
            }
			catch (...) {}
			return concurrency::task_from_result(Activities::MakeFaulted());
        }, token)
            .then([=](concurrency::task<Activities> result)
        {
            try
            {
                progress.report(0.99f);
                std::unordered_set<Platform::String^> toastedLookup(_alreadyToasted.begin(), _alreadyToasted.end());
                auto resultActivities = result.get();
				if (!resultActivities.Faulted)
				{
					JsonObject^ validationResult = nullptr;
					if (JsonObject::TryParse(resultActivities.Blob, &validationResult))
					{
						ReceivedBlob = resultActivities.Blob;
						for (auto toastTpl : resultActivities.Toastables)
						{
							if (toastedLookup.find(toastTpl->Key) == toastedLookup.end())
							{
								UpdateCountSinceActivity = 0;
								MakeToast(toastTpl->Key, toastTpl->Value, resultActivities.ContextBlobs->HasKey(toastTpl->Key) ? resultActivities.ContextBlobs->Lookup(toastTpl->Key) : nullptr, activatedHandler);
								toastedLookup.insert(toastTpl->Key);
								_alreadyToasted.insert(_alreadyToasted.begin(), toastTpl->Key);
							}
						}
						_lastUpdate = chrono::duration_cast<chrono::seconds>(chrono::system_clock::now().time_since_epoch());
						StoreState();
					}
				}
            }
            catch (...)
            {
            }
        }, token);
    });
}


