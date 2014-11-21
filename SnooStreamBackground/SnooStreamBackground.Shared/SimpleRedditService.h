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

    class SimpleRedditService
    {
    private:
        RedditOAuth _oAuth;
        concurrency::task<Platform::String^> SendGet(Platform::String^ url);
    public:
        SimpleRedditService(RedditOAuth oAuth);
        concurrency::task<bool> HasMail();
        concurrency::task<std::vector<Platform::String^>> GetNewMessages();
        concurrency::task<std::vector<std::tuple<Platform::String^, Platform::String^>>> GetPostsBySubreddit(Platform::String^ subreddit, int limit);

    };
}