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
        Platform::String^ Blob;
        Platform::Collections::Map<Platform::String^, Platform::String^>^ Toastables;
    };

    class SimpleRedditService
    {
    private:
        RedditOAuth _oAuth;
        concurrency::task<Platform::String^> SendGet(Platform::String^ url);
    public:
        SimpleRedditService(RedditOAuth oAuth);
        concurrency::task<bool> HasMail();
        concurrency::task<Activities> GetMessages();
        concurrency::task<Activities> GetActivity();
        concurrency::task<Activities> GetSent();
        concurrency::task<std::vector<std::tuple<Platform::String^, Platform::String^>>> GetPostsBySubreddit(Platform::String^ subreddit, int limit);

    };
}