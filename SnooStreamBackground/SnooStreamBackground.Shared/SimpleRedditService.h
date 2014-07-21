#pragma once

#include <ppltasks.h>
#include <tuple>

namespace SnooStreamBackground
{
	class SimpleRedditService
	{
  private:
    Platform::String^ _cookie;
    concurrency::task<Platform::String^> SendGet(Platform::String^ url);
	public:
		SimpleRedditService(Platform::String^ loginCookie);
		concurrency::task<bool> HasMail();
    concurrency::task<std::vector<Platform::String^>> GetNewMessages();
		concurrency::task<std::vector<std::tuple<Platform::String^, Platform::String^>>> GetPostsBySubreddit(Platform::String^ subreddit, int limit);
	
	};
}