#include "pch.h"
#include "LiveTileUtilities.h"
#include <chrono>
#include <boost/format.hpp>

using namespace SnooStreamBackground;
using namespace Windows::UI::Notifications;
using namespace Windows::Foundation::Collections;
using namespace Windows::Foundation;
using namespace std;
using namespace boost;

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
			Windows::Data::Xml::Dom::XmlDocument^ tileTemplate = ref new Windows::Data::Xml::Dom::XmlDocument();

			switch (liveTile->LiveTileStyle)
			{
				case LiveTileStyle::ImageSet:
				{
					break;
				}
				case LiveTileStyle::Image:
				{
          auto imageTileXml = str(wformat(
            L"<tile>"
              "<visual>"
                "<binding template = \"TileMedium\" branding = \"name\">"
                  "<image placement = \"background\" src = \"ms-appdata:///Local/%s\" />"
                "</binding>"
                "<binding template = \"TileWide\" branding = \"name\">"
                  "<image placement = \"background\" src = \"ms-appdata:///Local/%s\" />"
                "</binding>"
              "</visual>"
            "</tile>") % option->LocalSmallSquareUrl->Data() % option->LocalWideUrl->Data());

          tileTemplate->LoadXml(ref new Platform::String(imageTileXml.c_str(), imageTileXml.length()));
					break;
				}
				case LiveTileStyle::Text:
				{
					break;
				}
				case LiveTileStyle::TextImage:
				{
          auto textImageTileXml = str(wformat(
            L"<tile>"
              "<visual>"
                "<binding template = \"TileMedium\" branding = \"name\">"
                  "<image placement = \"peek\" src = \"ms-appdata:///Local/%s\" />"
                  "<text hint-style = \"captionsubtle\" hint-wrap = \"true\">%s</text>"
                "</binding>"
                "<binding template = \"TileWide\" branding = \"name\">"
                  "<image placement = \"background\" src = \"ms-appdata:///Local/%s\" />"
                  "<text hint-style = \"captionsubtle\" hint-wrap = \"true\">%s</text>"
                "</binding>"
              "</visual>"
            "</tile>") % option->LocalSmallSquareUrl->Data() % option->Title->Data() %  option->LocalWideUrl->Data() % option->Title->Data());
          tileTemplate->LoadXml(ref new Platform::String(textImageTileXml.c_str(), textImageTileXml.length()));
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
                auto tagString = std::to_wstring(tagId);
                tile->Tag = ref new Platform::String(tagString.c_str(), tagString.size());
				updater->Update(tile);
                auto setting = updater->Setting;
			}
		}
	}

}