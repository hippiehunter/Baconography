#include "pch.h"
#include "LiveTileUtilities.h"
#include <chrono>
using namespace SnooStreamBackground;
using namespace Windows::UI::Notifications;
using namespace Windows::Foundation::Collections;
using namespace Windows::Foundation;
using namespace std;

void LiveTileUtilities::MakeLiveTile(LockScreenHistory^ history, LiveTileSettings^ liveTile, IVector<ImageInfo^>^ options, Windows::UI::Notifications::TileUpdater^ updater)
{
	vector<ImageInfo^> stdOptions(begin(options), end(options));
	MakeLiveTile(history, liveTile, stdOptions, updater);
}

DateTime toFileTime(std::chrono::seconds seconds)
{
	long long unixTime = seconds.count();
	DateTime result;
	result.UniversalTime = unixTime * 10000000ULL + 116444736000000000LL;
	return result;
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
			Windows::Data::Xml::Dom::XmlDocument^ tileTemplate = nullptr;

			switch (liveTile->LiveTileStyle)
			{
				case LiveTileStyle::ImageSet:
				{
					break;
				}
				case LiveTileStyle::Image:
				{
					tileTemplate = Windows::UI::Notifications::TileUpdateManager::GetTemplateContent(Windows::UI::Notifications::TileTemplateType::TileWide310x150Image);
					auto tileAttributes = tileTemplate->GetElementsByTagName("image");
					tileAttributes->GetAt(0)->Attributes->GetNamedItem("src")->InnerText = "ms-appdata:///local/" + option->LocalWideUrl;

					auto squareTemplate = Windows::UI::Notifications::TileUpdateManager::GetTemplateContent(Windows::UI::Notifications::TileTemplateType::TileSquare150x150Image);
					auto squareAttributes = squareTemplate->GetElementsByTagName("image");
					squareAttributes->GetAt(0)->Attributes->GetNamedItem("src")->InnerText = "ms-appdata:///local/" + option->LocalSmallSquareUrl;

					auto node = tileTemplate->ImportNode(squareTemplate->GetElementsByTagName("binding")->Item(0), true);
					tileTemplate->GetElementsByTagName("visual")->Item(0)->AppendChild(node);
					break;
				}
				case LiveTileStyle::Text:
				{
					tileTemplate = Windows::UI::Notifications::TileUpdateManager::GetTemplateContent(Windows::UI::Notifications::TileTemplateType::TileWide310x150Text04);
					tileTemplate->GetElementsByTagName("text")->Item(0)->AppendChild(tileTemplate->CreateTextNode(option->Title));
					auto squareTemplate = Windows::UI::Notifications::TileUpdateManager::GetTemplateContent(Windows::UI::Notifications::TileTemplateType::TileSquare150x150Text04);
					squareTemplate->GetElementsByTagName("text")->Item(0)->AppendChild(squareTemplate->CreateTextNode(option->Title));

					auto node = tileTemplate->ImportNode(squareTemplate->GetElementsByTagName("binding")->Item(0), true);
					tileTemplate->GetElementsByTagName("visual")->Item(0)->AppendChild(node);
					break;
				}
				case LiveTileStyle::TextImage:
				{
					tileTemplate = Windows::UI::Notifications::TileUpdateManager::GetTemplateContent(Windows::UI::Notifications::TileTemplateType::TileWide310x150ImageAndText01);
					auto tileAttributes = tileTemplate->GetElementsByTagName("image");
					tileTemplate->GetElementsByTagName("text")->Item(0)->AppendChild(tileTemplate->CreateTextNode(option->Title));
					tileAttributes->GetAt(0)->Attributes->GetNamedItem("src")->InnerText = "ms-appdata:///local/" + option->LocalWideUrl;

					auto squareTemplate = Windows::UI::Notifications::TileUpdateManager::GetTemplateContent(Windows::UI::Notifications::TileTemplateType::TileSquare150x150PeekImageAndText04);
					auto squareAttributes = squareTemplate->GetElementsByTagName("image");
					squareTemplate->GetElementsByTagName("text")->Item(0)->AppendChild(squareTemplate->CreateTextNode(option->Title));
					squareAttributes->GetAt(0)->Attributes->GetNamedItem("src")->InnerText = "ms-appdata:///local/" + option->LocalSmallSquareUrl;

					auto node = tileTemplate->ImportNode(squareTemplate->GetElementsByTagName("binding")->Item(0), true);
					tileTemplate->GetElementsByTagName("visual")->Item(0)->AppendChild(node);
					break;
				}
			}


			if (tagId++ > 5)
			{
				//TODO this needs toFileTime + some chrono work
				auto targetSeconds = tagId / 5 * (60 * 5);
				auto updateSchedule = toFileTime(std::chrono::seconds(time(nullptr) + targetSeconds));
				auto tile = ref new Windows::UI::Notifications::ScheduledTileNotification(tileTemplate, updateSchedule);
				auto tagString = std::to_wstring(tagId);
				tile->Tag = ref new Platform::String(tagString.c_str(), tagString.size());
				updater->AddToSchedule(tile);
			}
			else
			{
				auto tile = ref new Windows::UI::Notifications::TileNotification(tileTemplate);
				updater->Update(tile);
			}
		}
	}

}