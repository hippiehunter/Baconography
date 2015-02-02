#pragma once

#include <ppltasks.h>
#include <tuple>
#include <chrono>

namespace SnooStreamBackground
{
    class RedditOAuth
    {
    public:
        static RedditOAuth Deserialize(Platform::String^ json);
        static Platform::String^ Serialize(RedditOAuth oAuth);

        Platform::String^ Username;
        Platform::String^ AccessToken;
        Platform::String^ TokenType;
        int ExpiresIn;
        std::chrono::seconds Created;
        Platform::String^ Scope;
        Platform::String^ RefreshToken;
    };

    class Activities
    {
    public:
		bool Faulted;
        Platform::String^ Blob;
        Platform::Collections::Map<Platform::String^, Platform::String^>^ Toastables;
        Platform::Collections::Map<Platform::String^, Platform::String^>^ NameContextMapping;
        Platform::Collections::Map<Platform::String^, Platform::String^>^ ContextBlobs;
		static Activities MakeFaulted() 
		{
			Activities result;
			result.Faulted = true;
			result.Blob = nullptr;
			result.Toastables = nullptr;
			result.NameContextMapping = nullptr;
			result.ContextBlobs = nullptr;
			return result;
		}
    };

    class SimpleRedditService
    {
    private:
        RedditOAuth _oAuth;
        concurrency::task<Platform::String^> SendGet(Platform::String^ url);
        concurrency::task<Activities> ProcContext(concurrency::task<Activities> activitiesTask);
		concurrency::task<Platform::String^> SendGetBody(Windows::Web::Http::HttpClient^ httpClient, Platform::String^ localUrl);
    public:
        SimpleRedditService(RedditOAuth oAuth);
        concurrency::task<bool> HasMail();
        concurrency::task<Activities> GetMessages();
        concurrency::task<Activities> GetActivity();
        concurrency::task<Activities> GetSent();
        concurrency::task<std::vector<std::tuple<Platform::String^, Platform::String^>>> GetPostsBySubreddit(Platform::String^ subreddit, int limit);

    };
}