#include "pch.h"
#include "LockScreenHistory.h"

#include <iostream>
#include <fstream>
#include <string>
#include <sstream>

using namespace SnooStreamBackground;
using namespace std;
using Windows::Data::Json::JsonObject;
using Windows::Data::Json::JsonValue;
using Windows::Data::Json::JsonArray;
using Platform::String;

using namespace Windows::UI::Notifications;
using namespace Windows::Data::Xml::Dom;

LockScreenHistory::LockScreenHistory()
{
    wstring localPath(Windows::Storage::ApplicationData::Current->LocalFolder->Path->Data());
    localPath += L"\\bgtaskHistory.txt";
    wifstream settingsFile(localPath, std::ios_base::in | std::ios_base::binary, _SH_DENYRW);

	wstring fileStr;
	if (settingsFile.is_open())
	{
		fileStr = wstring(istreambuf_iterator<wchar_t>(settingsFile), istreambuf_iterator<wchar_t>());
	}

    settingsFile.close();

    _history = ref new Platform::Collections::UnorderedMap<Platform::String^, int>();
    if (fileStr.size() > 0)
    {
        auto parsedFileObject = JsonObject::Parse(ref new String(fileStr.data(), fileStr.size()));

        auto currentTileImages = parsedFileObject->GetNamedArray("CurrentTileImages");
        auto imageHistory = parsedFileObject->GetNamedArray("ImageHistory");
        auto lockScreenImages = parsedFileObject->GetNamedArray("LockScreenImages");
        for each (auto&& image in imageHistory)
        {
            auto imageObject = image->GetObject();
            auto originalUrl = imageObject->GetNamedString("OriginalUrl");
            auto lastShown = static_cast<int>(imageObject->GetNamedNumber("LastShown", 0));

            if (!_history->HasKey(originalUrl))
            {
                _history->Insert(originalUrl, lastShown);
            }
        }

        auto makeTileImage = [](JsonArray^ targetArray)
        {
            auto result = ref new Platform::Collections::Vector<ImageInfo^>();

            for each (auto&& image in targetArray)
            {
                try
                {
                    auto imageObject = image->GetObject();
                    auto originalUrl = imageObject->GetNamedString("OriginalUrl");
                    auto lastShown = static_cast<int>(imageObject->GetNamedNumber("LastShown", 0));
                    auto localSmallSquareUrl = imageObject->GetNamedString("LocalSmallSquareUrl", nullptr);
                    auto localLargeSquareUrl = imageObject->GetNamedString("LocalLargeSquareUrl", nullptr);
                    auto localWideUrl = imageObject->GetNamedString("LocalWideUrl", nullptr);
                    auto title = imageObject->GetNamedString("Title", "");
                    result->Append(ref new ImageInfo(originalUrl, localSmallSquareUrl, localLargeSquareUrl, localWideUrl, lastShown, title));
                }
                catch (...) { }
            }
            return result;
        };

        CurrentTileImages = makeTileImage(currentTileImages);
        LockScreenImages = makeTileImage(lockScreenImages);

    }
}

int LockScreenHistory::Age(Platform::String^ url)
{
    if (_history->HasKey(url))
        return _history->Lookup(url);
    else
        return INT_MAX;
}

void LockScreenHistory::Store()
{
    auto serializedObject = ref new JsonObject();
    auto imageHistory = ref new JsonArray();
    auto currentTileImages = ref new JsonArray();
    auto lockScreenImages = ref new JsonArray();
    for each(auto&& imageTpl in _history)
    {
        auto serializedImage = ref new JsonObject();

        serializedImage->Insert("OriginalUrl", JsonValue::CreateStringValue(imageTpl->Key));
        serializedImage->Insert("LastShown", JsonValue::CreateNumberValue(imageTpl->Value));
        imageHistory->Append(serializedImage);
    }

    auto serializeImageInfo = [](ImageInfo^ image, JsonArray^ targetArray)
    {
        auto serializedImage = ref new JsonObject();

        serializedImage->Insert("OriginalUrl", JsonValue::CreateStringValue(image->OriginalUrl));
        serializedImage->Insert("Title", JsonValue::CreateStringValue(image->Title));
        serializedImage->Insert("LocalWideUrl", JsonValue::CreateStringValue(image->LocalWideUrl));
        serializedImage->Insert("LocalLargeSquareUrl", JsonValue::CreateStringValue(image->LocalLargeSquareUrl));
        serializedImage->Insert("LocalSmallSquareUrl", JsonValue::CreateStringValue(image->LocalSmallSquareUrl));
        serializedImage->Insert("LastShown", JsonValue::CreateNumberValue(image->LastShown));
        targetArray->Append(serializedImage);
    };

    if (CurrentTileImages != nullptr)
    {
        for each(auto image in CurrentTileImages)
        {
            serializeImageInfo(image, currentTileImages);
        }
    }

    if (LockScreenImages != nullptr)
    {
        for each(auto&& image in LockScreenImages)
        {
            serializeImageInfo(image, imageHistory);
        }
    }

    serializedObject->Insert("ImageHistory", imageHistory);
    serializedObject->Insert("CurrentTileImages", currentTileImages);
    serializedObject->Insert("LockScreenImages", lockScreenImages);
    serializedObject->Insert("LastLockScreenUpdate", JsonValue::CreateStringValue(LastLockScreenUpdate));


    auto serializedString = serializedObject->Stringify();
    wstring localPath(Windows::Storage::ApplicationData::Current->LocalFolder->Path->Data());
    localPath += L"\\bgtaskHistory.txt";
    wofstream settingsFile(localPath, std::ios_base::out | std::ios_base::binary | std::ios_base::trunc, _SH_DENYRW);
    wstring settingsFileString(serializedString->Data(), serializedString->Length());
    settingsFile << settingsFileString;
    settingsFile.close();
}