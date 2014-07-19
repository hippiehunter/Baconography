#include "pch.h"
#include "SimpleRedditService.h"
#include <iostream>
#include <fstream>
#include <string>
#include "boost\format.hpp"

using Windows::Web::Http::HttpClient;
using namespace SnooStreamBackground;
using concurrency::task;
using Platform::String;
using Windows::Foundation::Uri;
using Windows::Data::Json::JsonObject;
using std::vector;
using std::tuple;
using std::make_tuple;
using std::begin;
using std::end;
using std::wstring;
using std::wifstream;
using std::wofstream;
using std::getline;

concurrency::task<Platform::String^> SendGet(String^ url)
{
  auto httpClient = ref new HttpClient();
  httpClient->DefaultRequestHeaders->Cookie->Append(ref new Windows::Web::Http::Headers::HttpCookiePairHeaderValue("reddit_session", _cookie);
  return task<String^>(httpClient->GetStringAsync(ref new Uri(url)));
}
SimpleRedditService::SimpleRedditService(String^ loginCookie)
{
  _cookie = loginCookie;
}

task<bool> SimpleRedditService::HasMail()
{
  return SendGet("https://www.reddit.com/api/me.json")
    .then([](String^ meResponse)
	{
		auto meObject = JsonObject::Parse(meResponse);
		auto dataObject = meObject->GetNamedObject("data");
		return dataObject->GetNamedBoolean("has_mail");
	});
}


task<vector<String^>> SimpleRedditService::GetNewMessages()
{
  return SendGet("https://www.reddit.com/message/unread/.json")
    .then([&](String^ unreadResponse)
	{
		vector<String^> existingMessages;
		vector<String^> newMessages;
		vector<String^> displayTitles;

		try
		{
			wstring localPath(Windows::Storage::ApplicationData::Current->LocalFolder->Path->Data());
			localPath += L"bgtaskMessages.txt";
			wifstream existingMessagesFile(localPath);
			wstring existingMessageLine;
			while (getline(existingMessagesFile, existingMessageLine))
			{
				auto newMessageId = ref new String(existingMessageLine.data(), existingMessageLine.size());
				if (find(begin(existingMessages), end(existingMessages), newMessageId) == end(existingMessages))
					existingMessages.push_back(newMessageId);
			}
			existingMessagesFile.close();

			auto messages = JsonObject::Parse(unreadResponse);
			auto messageArray = messages->GetNamedObject("data")->GetNamedArray("children");

			for (auto&& message : messageArray)
			{
				auto messageObject = message->GetObject();
				auto messageData = messageObject->GetNamedObject("data");
				auto messageNew = messageData->GetNamedBoolean("new");
				auto messageName = messageData->GetNamedString("name");

				if (messageNew)
				{
					if (std::find(begin(existingMessages), end(existingMessages), messageName) != end(existingMessages))
						continue;

					newMessages.push_back(messageName);
					auto messageSubject = messageObject->GetNamedString("subject");
					auto messageWasComment = messageObject->GetNamedBoolean("was_comment");
					if (messageWasComment)
					{
						displayTitles.push_back(messageObject->GetNamedString("link_title"));
					}
					else
					{
						displayTitles.push_back(messageSubject);

					}
				}
			}

			wofstream existingMessagesOutputFile(localPath);
			for (auto newMessage : newMessages)
			{
				existingMessagesOutputFile << newMessage->Data();
			}
			existingMessagesOutputFile.close();
		}
		catch(...) {}
		return newMessages;
	});
}

task<vector<tuple<String^, String^>>> SimpleRedditService::GetPostsBySubreddit(String^ subreddit, int limit)
{
	auto httpClient = ref new HttpClient();
	auto targetUrl = (boost::wformat(L"http://www.reddit.com%1%.json?limit=%2%") % subreddit->Data() % limit).str();
  return SendGet(ref new String(targetUrl.data(), targetUrl.size()))
    .then([&](String^ postsResponse)
	{
		vector<tuple<String^, String^>> result;

		auto posts = JsonObject::Parse(postsResponse);
		auto postArray = posts->GetNamedObject("data")->GetNamedArray("children");

		for (auto&& post : postArray)
		{
			auto postObject = post->GetObject();
			auto postData = postObject->GetNamedObject("data");
			if (postData->GetNamedBoolean("over_18"))
				continue;

			result.emplace_back(postData->GetNamedString("title"), postData->GetNamedBoolean("is_self") ? "" : postData->GetNamedString("url"));
		}

		return result;
	});
}