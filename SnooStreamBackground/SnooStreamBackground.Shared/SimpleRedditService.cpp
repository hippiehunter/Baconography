#include "pch.h"
#include "SimpleRedditService.h"
#include <iostream>
#include <fstream>
#include <string>
#include <chrono>
#include <sstream>
#include "boost\format.hpp"
#include "boost\archive\iterators\base64_from_binary.hpp"
#include "boost\archive\iterators\ostream_iterator.hpp"
#include "boost\archive\iterators\transform_width.hpp"
#include "boost\utility\string_ref.hpp"


using Windows::Web::Http::HttpClient;
using namespace SnooStreamBackground;
using concurrency::task;
using concurrency::task_from_result;
using concurrency::create_task;
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
using boost::wstring_ref;

std::string toStdString(Platform::String^ str)
{
    std::string result;
    result.resize(str->Length() * 2);
    auto length = WideCharToMultiByte(CP_UTF8, 0, str->Data(), str->Length(), (char*) &result[0], result.size(), NULL, NULL);
    result.resize(length);
    return result;
}

std::string toBase64(const std::string& str)
{
    std::stringstream os;
    typedef boost::archive::iterators::base64_from_binary<boost::archive::iterators::transform_width<const char *, 6, 8>> base64_text;
    std::copy(base64_text(str.c_str()), base64_text(str.c_str()), boost::archive::iterators::ostream_iterator<char>(os));
    return os.str();
}

Platform::String^ toPlatformString(const std::string& str)
{
    int len;
    int slength = (int) str.length() + 1;
    len = MultiByteToWideChar(CP_UTF8, 0, str.c_str(), slength, 0, 0);
    wchar_t* buf = new wchar_t[len];
    MultiByteToWideChar(CP_UTF8, 0, str.c_str(), slength, buf, len);
    auto result = ref new Platform::String(buf, len);
    delete [] buf;
    return result;
}

RedditOAuth RedditOAuth::Deserialize(Platform::String^ data)
{
    if (data->Length() > 0)
    {
        auto userObject = JsonObject::Parse(data);
        auto oAuthObject = userObject->GetNamedObject("OAuth");
        return RedditOAuth
        {
            userObject->GetNamedString("Username"),
            oAuthObject->GetNamedString("access_token"),
            oAuthObject->GetNamedString("token_type"),
            static_cast<int>(oAuthObject->GetNamedNumber("expires_in")),
            std::chrono::seconds(static_cast<int>(oAuthObject->GetNamedNumber("created"))),
            oAuthObject->GetNamedString("scope"),
            oAuthObject->GetNamedString("refresh_token")
        };
    }
    else
        return RedditOAuth {};
}


concurrency::task<RedditOAuth> RefreshToken(Platform::String^ refreshToken, Platform::String^ username)
{
    //we're messing with the headers here so use a different client
    auto httpClient = ref new HttpClient();
    httpClient->DefaultRequestHeaders->UserAgent->ParseAdd("SnooStream/1.0");
    httpClient->DefaultRequestHeaders->Authorization = ref new Windows::Web::Http::Headers::HttpCredentialsHeaderValue(L"Basic", L"M205clF0QmluT2dfckE6");
    auto encodedContent = ref new Platform::Collections::Map<Platform::String^, Platform::String^>();
    encodedContent->Insert(L"grant_type", L"refresh_token");
    encodedContent->Insert(L"refresh_token", refreshToken);

    return create_task(httpClient->PostAsync(ref new Uri(L"https://ssl.reddit.com/api/v1/access_token"), ref new Windows::Web::Http::HttpFormUrlEncodedContent(encodedContent)))
        .then([=](task<Windows::Web::Http::HttpResponseMessage^> responseTask)
    {
        try
        {
            auto response = responseTask.get();
            return create_task(response->Content->ReadAsStringAsync())
                .then([=](task<Platform::String^> stringResultTask)
            {
                try
                {
                    auto stringResult = stringResultTask.get();
                    auto oAuthObject = JsonObject::Parse(stringResult);

                    return RedditOAuth
                    {
                        username,
                        oAuthObject->GetNamedString("access_token"),
                        oAuthObject->GetNamedString("token_type"),
                        static_cast<int>(oAuthObject->GetNamedNumber("expires_in")),
                        std::chrono::seconds(time(nullptr)),
                        oAuthObject->GetNamedString("scope"),
                        refreshToken
                    };
                }
                catch (...)
                {
                    return RedditOAuth{};
                }
            });
        }
        catch (...)
        {
            return task_from_result(RedditOAuth{});
        }
    });
}

bool starts_with(wstring_ref fullString, wstring_ref start)
{
    if (fullString.length() >= start.length())
    {
        return (0 == wstring_ref(fullString.data(), start.length()).compare(start));
    }
    else
    {
        return false;
    }
}

bool ends_with(wstring_ref fullString, wstring_ref ending)
{
    if (fullString.length() >= ending.length())
    {
        return (0 == wstring_ref((fullString.data() + (fullString.length() - ending.length())), ending.length()).compare(ending));
    }
    else
    {
        return false;
    }
}

concurrency::task<Platform::String^> SimpleRedditService::SendGetBody(HttpClient^ httpClient, String^ localUrl)
{
	return create_task(httpClient->GetAsync(ref new Uri(localUrl)))
		.then([=](task<Windows::Web::Http::HttpResponseMessage^> responseTask)
	{
		try
		{
			auto response = responseTask.get();
			return create_task(response->Content->ReadAsStringAsync())
				.then([=](task<Platform::String^> resultTask)
			{
				try
				{
					auto result = resultTask.get();
					wstring_ref resultString(result->Data(), result->Length());
					//if reddit says try again, try again
					if (starts_with(resultString, L"<!doctype html><html><title>") &&
						ends_with(resultString, L"try again and hopefully we will be fast enough this time."))
					{
						return SendGetBody(httpClient, localUrl);
					}
					else if (starts_with(resultString, L"<!doctype html><html><title>"))
						return concurrency::task_from_result((Platform::String^)nullptr);
					else
					{
						response->EnsureSuccessStatusCode();
						return concurrency::task_from_result(result);
					}
				}
				catch (...)
				{
					return concurrency::task_from_result((Platform::String^)nullptr);
				}
			});
		}
		catch (...)
		{
			return concurrency::task_from_result((Platform::String^)nullptr);
		}
	});
}

concurrency::task<Platform::String^> SimpleRedditService::SendGet(String^ url)
{
  auto httpClient = ref new HttpClient();
  httpClient->DefaultRequestHeaders->UserAgent->ParseAdd("SnooStream/1.0");

  //see if we need to refresh the token
  if (_oAuth.AccessToken != nullptr)
  {
      return create_task(RefreshToken(_oAuth.RefreshToken, _oAuth.Username))
          .then([=](task<RedditOAuth> oAuthTask)
      {
		  auto localUrl = url;
		  try
		  {
			  auto oAuth = oAuthTask.get();
			  if (oAuth.AccessToken != nullptr)
			  {
				  _oAuth = oAuth;
				  httpClient->DefaultRequestHeaders->Authorization = ref new Windows::Web::Http::Headers::HttpCredentialsHeaderValue("Bearer", _oAuth.AccessToken);
				  localUrl = "https://oauth.reddit.com" + localUrl;
			  }
			  else
			  {
				  localUrl = "http://reddit.com" + localUrl;
			  }
		  }
		  catch (...)
		  {
			  localUrl = "http://reddit.com" + localUrl;
		  }
		  return SendGetBody(httpClient, localUrl);
          
      });
  }
  else
  {
      url = "http://reddit.com" + url;
	  return SendGetBody(httpClient, url);
  }
}

SimpleRedditService::SimpleRedditService(RedditOAuth oAuth)
{
    _oAuth = oAuth;
}

task<bool> SimpleRedditService::HasMail()
{
  return SendGet("/api/me.json")
    .then([](String^ meResponse)
	{
        if (meResponse != nullptr)
        {
            auto meObject = JsonObject::Parse(meResponse);
            auto dataObject = meObject->GetNamedObject("data");
            return dataObject->GetNamedBoolean("has_mail");
        }
        else
            return false;
	});
}

concurrency::task<Activities> SimpleRedditService::ProcContext(concurrency::task<Activities> activitiesTask)
{
    try
    {
        auto activities = activitiesTask.get();
        for (auto context : activities.ContextBlobs)
        {
            if (context->Value->Length() == 0)
            {
                return SendGet(activities.NameContextMapping->Lookup(context->Key))
                    .then([=](String^ response)
                {
                    activities.ContextBlobs->Remove(context->Key);
                    if (response != nullptr && response->Length() > 0)
                        activities.ContextBlobs->Insert(context->Key, response);

                    return concurrency::task_from_result(activities);
                }).then([=](concurrency::task<Activities> placeholder) { return ProcContext(placeholder); });
            }
        }
        return concurrency::task_from_result(activities);
    }
    catch (...)
    {
        return activitiesTask;
    }
}

concurrency::task<Activities> SimpleRedditService::GetMessages()
{
    auto contextBlobs = ref new Platform::Collections::Map<Platform::String^, Platform::String^>();
    auto nameContextBlob = ref new Platform::Collections::Map<Platform::String^, Platform::String^>();
    return SendGet("/message/inbox/.json")
        .then([=](String^ response)
    {
		bool faulted = false;
        auto toastables = ref new Platform::Collections::Map<Platform::String^, Platform::String^>();
		if (response != nullptr)
		{
			auto messages = JsonObject::Parse(response);
			auto messageArray = messages->GetNamedObject("data")->GetNamedArray("children");

			for (auto&& message : messageArray)
			{
				try
				{
					auto messageObject = message->GetObject();
					auto messageData = messageObject->GetNamedObject("data");
					auto messageId = messageData->GetNamedString("id");
					auto messageNew = messageData->GetNamedBoolean("new");
					auto messageName = messageData->GetNamedString("name");

					if (messageNew)
					{
						String^ toastableContent = nullptr;
						auto messageWasComment = messageData->GetNamedBoolean("was_comment");
						if (messageWasComment)
						{
							contextBlobs->Insert(messageName, "");
							nameContextBlob->Insert(messageName, messageData->GetNamedString("context"));
							toastableContent = messageData->GetNamedString("link_title");
						}
						else
							toastableContent = messageData->GetNamedString("subject");

						toastables->Insert(messageName, toastableContent);

					}
				}
				//ignore bad messages (probably an odd thing type in the listing)
				catch (...) { faulted = true; }
			}
		}
		else
			faulted = true;

        Activities result = { faulted, response, toastables, nameContextBlob, contextBlobs};
        return result;
    }).then([=](concurrency::task<Activities> placeholder) { return ProcContext(placeholder); });
}

concurrency::task<Activities> SimpleRedditService::GetActivity()
{
    return SendGet("/user/" + _oAuth.Username + "/.json")
        .then([&](String^ response)
    {
        Activities result = { response == nullptr, response, nullptr };
        return result;
    });
}

concurrency::task<Activities> SimpleRedditService::GetSent()
{
    return SendGet("/message/sent/.json")
        .then([&](String^ response)
    {
        Activities result = { response == nullptr, response, nullptr };
        return result;
    });
}

task<vector<tuple<String^, String^>>> SimpleRedditService::GetPostsBySubreddit(String^ subreddit, int limit)
{
	auto httpClient = ref new HttpClient();
	auto targetUrl = (boost::wformat(L"%1%.json?limit=%2%") % subreddit->Data() % limit).str();
  return SendGet(ref new String(targetUrl.data(), targetUrl.size()))
    .then([&](String^ postsResponse)
	{
		vector<tuple<String^, String^>> result;
		if (postsResponse == nullptr)
			return concurrency::task_from_exception<vector<tuple<String^, String^>>>(ref new Platform::Exception(1337));

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

		return concurrency::task_from_result(result);
	});
}