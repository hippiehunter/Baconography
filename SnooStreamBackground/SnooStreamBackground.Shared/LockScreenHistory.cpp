#include "pch.h"
#include "LockScreenHistory.h"

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
using Windows::Data::Json::JsonValue;
using Windows::Data::Json::JsonArray;
using Platform::String;
using boost::lexical_cast;

LockScreenHistory::LockScreenHistory()
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
  settingsFile.close();

  auto fileStr = fileContents.str();
  auto parsedFileObject = JsonObject::Parse(ref new String(fileStr.data(), fileStr.size()));

  auto currentTileImages = parsedFileObject->GetNamedArray("CurrentTileImages");
  auto imageHistory = parsedFileObject->GetNamedArray("ImageHistory");
  auto lockScreenImages = parsedFileObject->GetNamedArray("LockScreenImages");
  _history = ref new Platform::Collections::UnorderedMap<Platform::String^, Platform::String^>();
  for each (auto&& image in imageHistory)
  {
    auto imageObject = image->GetObject();
    auto originalUrl = imageObject->GetNamedString("OriginalUrl");
    auto lastShown = imageObject->GetNamedString("LastShown");

    if (!_history->HasKey(originalUrl))
    {
      _history->Insert(originalUrl, lastShown);
    }
  } 

  CurrentTileImages = ref new Platform::Collections::Vector<LockScreenImageInfo^>();
  LockScreenImages = ref new Platform::Collections::Vector<LockScreenImageInfo^>();

  for each (auto&& image in currentTileImages)
  {
    auto imageObject = image->GetObject();
    auto originalUrl = imageObject->GetNamedString("OriginalUrl");
    auto lastShown = imageObject->GetNamedString("LastShown");
    auto localUrl = imageObject->GetNamedString("LocalUrl");
    CurrentTileImages->Append(ref new LockScreenImageInfo(originalUrl, localUrl, lastShown));
  }

  for each (auto&& image in lockScreenImages)
  {
    auto imageObject = image->GetObject();
    auto originalUrl = imageObject->GetNamedString("OriginalUrl");
    auto lastShown = imageObject->GetNamedString("LastShown");
    auto localUrl = imageObject->GetNamedString("LocalUrl");
    LockScreenImages->Append(ref new LockScreenImageInfo(originalUrl, localUrl, lastShown));
  }
}                               

bool LockScreenHistory::HasHistory(Platform::String^ url)
{
  return _history->HasKey(url);
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
    serializedImage->Insert("LastShown", JsonValue::CreateStringValue(imageTpl->Value));
    imageHistory->Append(serializedImage);
  }

  for each(auto&& image in CurrentTileImages)
  {
    auto serializedImage = ref new JsonObject();

    serializedImage->Insert("OriginalUrl", JsonValue::CreateStringValue(image->OriginalUrl));
    serializedImage->Insert("LocalUrl", JsonValue::CreateStringValue(image->LocalUrl));
    serializedImage->Insert("LastShown", JsonValue::CreateStringValue(image->LastShown));
    imageHistory->Append(serializedImage);
  }

  for each(auto&& image in LockScreenImages)
  {
    auto serializedImage = ref new JsonObject();

    serializedImage->Insert("OriginalUrl", JsonValue::CreateStringValue(image->OriginalUrl));
    serializedImage->Insert("LocalUrl", JsonValue::CreateStringValue(image->LocalUrl));
    serializedImage->Insert("LastShown", JsonValue::CreateStringValue(image->LastShown));
    imageHistory->Append(serializedImage);
  }

  serializedObject->Insert("ImageHistory", imageHistory);
  serializedObject->Insert("CurrentTileImages", currentTileImages);
  serializedObject->Insert("LockScreenImages", lockScreenImages);
  serializedObject->Insert("LastLockScreenUpdate", JsonValue::CreateStringValue(LastLockScreenUpdate));


  auto serializedString = serializedObject->Stringify();
  wstring localPath(Windows::Storage::ApplicationData::Current->LocalFolder->Path->Data());
  localPath += L"bgtaskSettings.txt";
  wofstream settingsFile(localPath);
  wstring settingsFileString(serializedString->Data(), serializedString->Length());
  settingsFile << settingsFileString;
  settingsFile.close();
}