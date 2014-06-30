#include "pch.h"
#include "LockScreenSettings.h"

#include <iostream>
#include <fstream>
#include <string>
#include <sstream>

#include <boost/lexical_cast.hpp>


using namespace SnooStreamBackground;
using std::wstring;
using std::wifstream;
using std::wofstream;
using std::wstringstream;
using Windows::Data::Json::JsonObject;
using Platform::String;
using boost::lexical_cast;

LockScreenSettings::LockScreenSettings()
{
	wstring localPath(Windows::Storage::ApplicationData::Current->LocalFolder->Path->Data());
	localPath += L"bgtaskSettings.txt";
	wifstream settingsFile(localPath);
	wstringstream fileContents;
	wstring fileLine;

	while (!settingsFile.eof())
	{
		settingsFile >> fileLine;
		fileContents << fileLine;
	}

	auto fileStr = fileContents.str();
	auto parsedFileObject = JsonObject::Parse(ref new String(fileStr.data(), fileStr.size()));

	auto toString = [](SettingIdentifiers ident)
	{
		auto identString = lexical_cast<wstring>((int)ident);
		return ref new String(identString.data(), identString.size());
	};

	RedditCookie = parsedFileObject->GetNamedString(toString(SettingIdentifiers::RedditCookie),RedditCookie);
	LockScreenOverlayRoundedEdges = parsedFileObject->GetNamedBoolean(toString(SettingIdentifiers::LockScreenOverlayRoundedEdges), LockScreenOverlayRoundedEdges);
	LockScreenOverlayOpacity = parsedFileObject->GetNamedNumber(toString(SettingIdentifiers::LockScreenOverlayOpacity), LockScreenOverlayOpacity);
	LockScreenOverlayItemsCount = parsedFileObject->GetNamedNumber(toString(SettingIdentifiers::LockScreenOverlayItemsCount), LockScreenOverlayItemsCount);
	LockScreenOverlayItemsReddit = parsedFileObject->GetNamedString(toString(SettingIdentifiers::LockScreenOverlayItemsReddit), LockScreenOverlayItemsReddit);
	auto lockScreenImageUris = parsedFileObject->GetNamedArray(toString(SettingIdentifiers::LockScreenImageURIs));
	auto liveTileStyle = parsedFileObject->GetNamedString(toString(SettingIdentifiers::LiveTileStyle));
	LiveTileItemsReddit = parsedFileObject->GetNamedString(toString(SettingIdentifiers::LiveTileItemsReddit), LiveTileItemsReddit);
	auto liveTileImageUris = parsedFileObject->GetNamedArray(toString(SettingIdentifiers::LiveTileImageURIs));

	LockScreenImageURIs = ref new Platform::Collections::Map<String^, String^>();
	if (lockScreenImageUris != nullptr)
	{
		for (auto&& lockScreenImageUri : lockScreenImageUris)
		{
			auto imageUriObject = lockScreenImageUri->GetObject();
			LockScreenImageURIs->Insert(imageUriObject->GetNamedString("filePath"), imageUriObject->GetNamedString("originalUri"));
		}
	}

	LiveTileImageURIs = ref new Platform::Collections::Map<String^, String^>();
	if (liveTileImageUris != nullptr)
	{
		for (auto&& liveTileImageUri : liveTileImageUris)
		{
			auto imageUriObject = liveTileImageUri->GetObject();
			LiveTileImageURIs->Insert(imageUriObject->GetNamedString("filePath"), imageUriObject->GetNamedString("originalUri"));
		}
	}

	if (liveTileStyle != nullptr)
	{
		if (liveTileStyle == L"Off")
		{
			LiveTileStyle = LiveTileStyle::Off;
		}
	}

	//LockFileEx(existingMessagesFile)
}


void LockScreenSettings::WriteSettings()
{

}