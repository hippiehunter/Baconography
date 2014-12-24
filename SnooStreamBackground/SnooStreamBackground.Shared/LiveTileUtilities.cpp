#include "pch.h"
#include "LiveTileUtilities.h"

using namespace SnooStreamBackground;
using namespace Windows::UI::Notifications;
using namespace Windows::Foundation::Collections;
using namespace std;

void LiveTileUtilities::MakeLiveTile(LockScreenHistory^ history, LiveTileSettings^ liveTile, IVector<ImageInfo^>^ options, Windows::UI::Notifications::TileUpdater^ updater)
{
    vector<ImageInfo^> stdOptions(begin(options), end(options));
    MakeLiveTile(history, liveTile, stdOptions, updater);
}

void LiveTileUtilities::MakeLiveTile(LockScreenHistory^ history, LiveTileSettings^ liveTile, vector<ImageInfo^> options, TileUpdater^ updater)
{
    if (options.size() > 0)
    {
        updater->EnableNotificationQueue(true);
        updater->Clear();
        int tagId = 0;
        for (auto option : options)
        {
            if (tagId++ > 5)
            {
                auto updateSchedule = Windows::Foundation::DateTime{};
                auto targetSeconds = tagId / 5 * (60 * 5);
                switch (liveTile->LiveTileStyle)
                {
                    case LiveTileStyle::ImageSet:
                    {
                        break;
                    }
                    case LiveTileStyle::Image:
                    {
                        auto tileTemplate = Windows::UI::Notifications::TileUpdateManager::GetTemplateContent(Windows::UI::Notifications::TileTemplateType::TileWide310x150Image);
                        auto tileAttributes = tileTemplate->GetElementsByTagName("image");
                        tileAttributes->GetAt(0)->Attributes->GetNamedItem("src")->InnerText = "ms-appdata:///local/" + option->LocalWideUrl;
                        auto tile = ref new Windows::UI::Notifications::ScheduledTileNotification(tileTemplate, updateSchedule);
                        auto tagString = std::to_wstring(tagId);
                        tile->Tag = ref new Platform::String(tagString.c_str(), tagString.size());
                        updater->AddToSchedule(tile);

                        tileTemplate = Windows::UI::Notifications::TileUpdateManager::GetTemplateContent(Windows::UI::Notifications::TileTemplateType::TileSquare150x150Image);
                        tileAttributes = tileTemplate->GetElementsByTagName("image");
                        tileAttributes->GetAt(0)->Attributes->GetNamedItem("src")->InnerText = "ms-appdata:///local/" + option->LocalSmallSquareUrl;
                        tile = ref new Windows::UI::Notifications::ScheduledTileNotification(tileTemplate, updateSchedule);
                        tile->Tag = ref new Platform::String(tagString.c_str(), tagString.size());
                        updater->AddToSchedule(tile);
                        break;
                    }
                    case LiveTileStyle::Text:
                    {
                        break;
                    }
                    case LiveTileStyle::TextImage:
                    {
                        auto tileTemplate = Windows::UI::Notifications::TileUpdateManager::GetTemplateContent(Windows::UI::Notifications::TileTemplateType::TileWide310x150PeekImageAndText01);
                        auto tileText = tileTemplate->GetElementsByTagName("text");
                        //TODO fill this out
                        //tileText->GetAt(0)->AppendChild()
                        auto tileAttributes = tileTemplate->GetElementsByTagName("image");
                        tileAttributes->GetAt(0)->Attributes->GetNamedItem("src")->InnerText = "ms-appdata:///local/" + option->LocalWideUrl;
                        auto tile = ref new Windows::UI::Notifications::ScheduledTileNotification(tileTemplate, updateSchedule);
                        auto tagString = std::to_wstring(tagId);
                        tile->Tag = ref new Platform::String(tagString.c_str(), tagString.size());
                        updater->AddToSchedule(tile);

                        tileTemplate = Windows::UI::Notifications::TileUpdateManager::GetTemplateContent(Windows::UI::Notifications::TileTemplateType::TileSquare150x150PeekImageAndText01);
                        tileAttributes = tileTemplate->GetElementsByTagName("image");
                        tileAttributes->GetAt(0)->Attributes->GetNamedItem("src")->InnerText = "ms-appdata:///local/" + option->LocalSmallSquareUrl;
                        tile = ref new Windows::UI::Notifications::ScheduledTileNotification(tileTemplate, updateSchedule);
                        tagString = std::to_wstring(tagId);
                        tile->Tag = ref new Platform::String(tagString.c_str(), tagString.size());
                        updater->AddToSchedule(tile);
                        break;
                    }
                }
            }
            else
            {
                switch (liveTile->LiveTileStyle)
                {
                    case LiveTileStyle::ImageSet:
                    {
                        break;
                    }
                    case LiveTileStyle::Image:
                    {
                        auto tileTemplate = Windows::UI::Notifications::TileUpdateManager::GetTemplateContent(Windows::UI::Notifications::TileTemplateType::TileWide310x150Image);
                        auto tileAttributes = tileTemplate->GetElementsByTagName("image");
                        tileAttributes->GetAt(0)->Attributes->GetNamedItem("src")->InnerText = "ms-appdata:///local/" + option->LocalWideUrl;
                        auto tile = ref new Windows::UI::Notifications::TileNotification(tileTemplate);
                        auto tagString = std::to_wstring(tagId);
                        tile->Tag = ref new Platform::String(tagString.c_str(), tagString.size());
                        updater->Update(tile);

                        tileTemplate = Windows::UI::Notifications::TileUpdateManager::GetTemplateContent(Windows::UI::Notifications::TileTemplateType::TileSquare150x150Image);
                        tileAttributes = tileTemplate->GetElementsByTagName("image");
                        tileAttributes->GetAt(0)->Attributes->GetNamedItem("src")->InnerText = "ms-appdata:///local/" + option->LocalSmallSquareUrl;
                        tile = ref new Windows::UI::Notifications::TileNotification(tileTemplate);
                        tagString = std::to_wstring(tagId);
                        tile->Tag = ref new Platform::String(tagString.c_str(), tagString.size());
                        updater->Update(tile);
                        break;
                    }
                    case LiveTileStyle::Text:
                    {
                        break;
                    }
                    case LiveTileStyle::TextImage:
                    {
                        auto tileTemplate = Windows::UI::Notifications::TileUpdateManager::GetTemplateContent(Windows::UI::Notifications::TileTemplateType::TileWide310x150PeekImageAndText01);
                        auto tileText = tileTemplate->GetElementsByTagName("text");
                        //TODO fill this out
                        //tileText->GetAt(0)->AppendChild()
                        auto tileAttributes = tileTemplate->GetElementsByTagName("image");
                        tileAttributes->GetAt(0)->Attributes->GetNamedItem("src")->InnerText = "ms-appdata:///local/" + option->LocalWideUrl;
                        auto tile = ref new Windows::UI::Notifications::TileNotification(tileTemplate);
                        auto tagString = std::to_wstring(tagId);
                        tile->Tag = ref new Platform::String(tagString.c_str(), tagString.size());
                        updater->Update(tile);

                        tileTemplate = Windows::UI::Notifications::TileUpdateManager::GetTemplateContent(Windows::UI::Notifications::TileTemplateType::TileSquare150x150PeekImageAndText01);
                        tileAttributes = tileTemplate->GetElementsByTagName("image");
                        tileAttributes->GetAt(0)->Attributes->GetNamedItem("src")->InnerText = "ms-appdata:///local/" + option->LocalSmallSquareUrl;
                        tile = ref new Windows::UI::Notifications::TileNotification(tileTemplate);
                        tagString = std::to_wstring(tagId);
                        tile->Tag = ref new Platform::String(tagString.c_str(), tagString.size());
                        updater->Update(tile);
                        break;
                    }
                }
            }
        }
    }

}