#pragma once

#include <ppltasks.h>
#include <tuple>

namespace SnooStreamBackground
{
	class SimpleRedditService
	{
	public:
		SimpleRedditService(Platform::String^ username, Platform::String^ password, Platform::String^ loginCookie);
		concurrency::task<bool> HasMail();
		concurrency::task<std::vector<Platform::String^>> GetNewMessages();
		concurrency::task<std::vector<std::tuple<Platform::String^, Platform::String^>>> GetPostsBySubreddit(Platform::String^ subreddit, int limit);
	
	};
}