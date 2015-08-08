#include "pch.h"
#include "LockScreenSettings.h"
#include "FileUtilities.h"

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

LockScreenSettings::LockScreenSettings()
{
    wstring localPath(Windows::Storage::ApplicationData::Current->LocalFolder->Path->Data());
    localPath += L"\\bgtaskSettings.txt";
	auto fileStr = readFileWithLock(localPath);
    if (fileStr->Length() > 0)
    {
        auto parsedFileObject = JsonObject::Parse(fileStr);

        RedditOAuth = parsedFileObject->GetNamedString("RedditOAuth", RedditOAuth);
        LockScreenOverlayRoundedEdges = parsedFileObject->GetNamedBoolean("LockScreenOverlayRoundedEdges", LockScreenOverlayRoundedEdges);
        LockScreenOverlayOpacity = (int)parsedFileObject->GetNamedNumber("LockScreenOverlayOpacity", LockScreenOverlayOpacity);
        LockScreenOverlayItemsCount = (int)parsedFileObject->GetNamedNumber("LockScreenOverlayItemsCount", LockScreenOverlayItemsCount);
        LockScreenOverlayItemsReddit = parsedFileObject->GetNamedString("LockScreenOverlayItemsReddit", LockScreenOverlayItemsReddit);
        auto liveTileSettings = parsedFileObject->GetNamedArray("LiveTileSettings");
        if (liveTileSettings != nullptr)
        {
            LiveTileSettings = ref new Platform::Collections::Vector<SnooStreamBackground::LiveTileSettings^>();
            for each(auto&& liveTile in liveTileSettings)
            {
                auto liveTileObject = liveTile->GetObject();
                auto liveTileStyle = liveTileObject->GetNamedString("LiveTileStyle");
                auto liveTileReddit = liveTileObject->GetNamedString("LiveTileItemsReddit");
                auto currentImages = liveTileObject->GetNamedArray("CurrentImages");

                if (liveTileStyle == nullptr || liveTileReddit == nullptr)
                {
                    continue;
                }
                else
                {
                    auto madeLiveTileSettings = ref new SnooStreamBackground::LiveTileSettings();
                    if (liveTileStyle == L"Off")
                        madeLiveTileSettings->LiveTileStyle = SnooStreamBackground::LiveTileStyle::Off;
                    else if (liveTileStyle == L"ImageSet")
                        madeLiveTileSettings->LiveTileStyle = SnooStreamBackground::LiveTileStyle::ImageSet;
                    else if (liveTileStyle == L"Text")
                        madeLiveTileSettings->LiveTileStyle = SnooStreamBackground::LiveTileStyle::Text;
                    else if (liveTileStyle == L"TextImage")
                        madeLiveTileSettings->LiveTileStyle = SnooStreamBackground::LiveTileStyle::TextImage;
                    else
                        madeLiveTileSettings->LiveTileStyle = SnooStreamBackground::LiveTileStyle::Image;

                    madeLiveTileSettings->LiveTileItemsReddit = liveTileReddit;
                    madeLiveTileSettings->CurrentImages = ref new Platform::Collections::Vector<String^>();
                    auto currentImages = liveTileObject->GetNamedArray("CurrentImages");
                    for each(auto&& image in currentImages)
                    {
                        madeLiveTileSettings->CurrentImages->Append(image->GetString());
                    }
                    LiveTileSettings->Append(madeLiveTileSettings);
                }
            }

        }
    }

    //LockFileEx(existingMessagesFile)
}


void LockScreenSettings::Store()
{
    auto serializedObject = ref new JsonObject();

    serializedObject->Insert("RedditOAuth", JsonValue::CreateStringValue(RedditOAuth));
    serializedObject->Insert("LockScreenOverlayRoundedEdges", JsonValue::CreateBooleanValue(LockScreenOverlayRoundedEdges));
    serializedObject->Insert("LockScreenOverlayOpacity", JsonValue::CreateNumberValue(LockScreenOverlayOpacity));
    serializedObject->Insert("LockScreenOverlayItemsCount", JsonValue::CreateNumberValue(LockScreenOverlayItemsCount));
    serializedObject->Insert("LockScreenOverlayItemsReddit", JsonValue::CreateStringValue(LockScreenOverlayItemsReddit));

    auto liveTileSettings = ref new JsonArray();
    if (LiveTileSettings != nullptr)
    {
        for each(auto&& liveTile in LiveTileSettings)
        {
            auto liveTileObject = ref new JsonObject();
            auto currentImages = ref new JsonArray();
            liveTileObject->Insert("CurrentImages", currentImages);
            for each(auto&& image in liveTile->CurrentImages)
            {
                currentImages->Append(JsonValue::CreateStringValue(image));
            }

            liveTileObject->Insert("LiveTileItemsReddit", JsonValue::CreateStringValue(liveTile->LiveTileItemsReddit));
            liveTileObject->Insert("LiveTileStyle", JsonValue::CreateStringValue(liveTile->LiveTileStyle.ToString()));

            liveTileSettings->Append(liveTileObject);
        }
    }
    serializedObject->Insert("LiveTileSettings", liveTileSettings);

    auto serializedString = serializedObject->Stringify();
    wstring localPath(Windows::Storage::ApplicationData::Current->LocalFolder->Path->Data());
    localPath += L"\\bgtaskSettings.txt";
    writeFileWithLock(serializedString, localPath, true);
}