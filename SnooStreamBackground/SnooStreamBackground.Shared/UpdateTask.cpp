#include "pch.h"
#include "SimpleRedditService.h"
#include "LockScreenSettings.h"
#include "LockScreenHistory.h"
#include "LockScreenViewModel.h"
#include "LockScreenViewControl.xaml.h"
#include "LockScreenItemView.xaml.h"
#include "boost\algorithm\string.hpp"
#include <vector>
#include <tuple>
#include <iostream>
#include <fstream>
#include <string>

using namespace Windows::ApplicationModel::Background;
using namespace Windows::System::Threading;
using namespace Windows::UI::Xaml::Media::Imaging;
using concurrency::task;
using Platform::String;
using std::vector;
using std::tuple;
using std::begin;
using std::end;
using std::wstring;
using std::wifstream;
using std::wofstream;
using std::getline;


namespace SnooStreamBackground
{
	[Windows::Foundation::Metadata::WebHostHidden]
	public ref class UpdateBackgroundTask sealed : 
#ifdef WINDOWS_PHONE
		XamlRenderingBackgroundTask
#else
		public IBackgroundTask
#endif
	{
  public:
    std::unique_ptr<SimpleRedditService> redditService;
#ifdef WINDOWS_PHONE
	protected:
		void OnRun(IBackgroundTaskInstance^ taskInstance) override
#else
	public:
		virtual void Run(IBackgroundTaskInstance^ taskInstance)
#endif
		{
			auto deferral = taskInstance->GetDeferral();
      auto lockScreenSettings = ref new LockScreenSettings();
      auto lockScreenHistory = ref new LockScreenHistory();

      redditService = std::make_unique<SimpleRedditService>(lockScreenSettings->RedditCookie);


      auto tileUpdateTask = RunTileUpdater(lockScreenSettings, lockScreenHistory).then([&]()
			{
        deferral->Complete();
			});
		}
  public:
		UpdateBackgroundTask()
		{

		}
	private:
    task<void> RunPrimaryTileUpdater(LockScreenSettings^ settings, LockScreenHistory^ history, LiveTileSettings^ liveTile)
    {
      return redditService->GetPostsBySubreddit(settings->LockScreenOverlayItemsReddit, 100)
        .then([&](std::vector<tuple<String^, String^>> messages)
      {
        vector<String^> liveTileImageUrls;
        for (auto& message : messages)
        {
          std::wstring url(std::get<1>(message)->Begin(), std::get<1>(message)->End());
          if(boost::ends_with(url, ".jpg") ||
            boost::ends_with(url, ".jpeg") ||
            boost::ends_with(url, ".png"))
          {
            liveTileImageUrls.push_back(std::get<1>(message));
            if (liveTileImageUrls.size() > 20)
              break;
          }
        }
      });
    }

    task<void> RunSecondaryTileUpdater(LockScreenSettings^ settings, LockScreenHistory^ history, LiveTileSettings^ liveTile)
    {
      return task<void>();
    }

    task<void> RunTileUpdater(LockScreenSettings^ settings, LockScreenHistory^ history)
    {
      LockScreenViewModel^ lockScreenViewModel = ref new LockScreenViewModel();

      lockScreenViewModel->ImageSource = lockScreenImage;
      lockScreenViewModel->OverlayOpacity = settings->LockScreenOverlayOpacity / 100.0f;
      lockScreenViewModel->NumberOfItems = settings->LockScreenOverlayItemsCount;
      lockScreenViewModel->RoundedCorners = settings->LockScreenOverlayRoundedEdges;
      lockScreenViewModel->OverlayItems = ref new Platform::Collections::Vector<LockScreenMessage^>();

      return redditService->GetNewMessages().then([&](std::vector<tuple<String^, String^>> messages)
      {
        if (messages.size() > 0)
        {
          for (auto&& message : messages)
          {
            if (history->Toast(std::get<0>(message), std::get<1>(message)))
            {
              lockScreenViewModel->OverlayItems->Append(ref new LockScreenMessage(std::get<1>(message), "\uE119"));
            }
          }
        }
      }).then([&]()
      {
        return redditService->GetPostsBySubreddit(settings->LockScreenOverlayItemsReddit, 10);
      }).then([&](std::vector<tuple<String^, String^>> redditItems)
      {
        for (auto&& message : redditItems)
        {
          if (lockScreenViewModel->OverlayItems->Size > (settings->LockScreenOverlayItemsCount - 1))
            break;

          lockScreenViewModel->OverlayItems->Append(ref new LockScreenMessage(std::get<0>(message), GetGlyph(std::get<1>(message))));
        }
      }).then([&]()
      {
        if (settings->LiveTileSettings->Size == 1)
          return RunPrimaryTileUpdater(settings, history, settings->LiveTileSettings->GetAt(0));
        else
        {
          auto tsk = RunPrimaryTileUpdater(settings, history, settings->LiveTileSettings->GetAt(0));
          for (int i = 1; i < settings->LiveTileSettings->Size; i++)
          {
            tsk = tsk.then([&]()
            {
              return RunSecondaryTileUpdater(settings, history, settings->LiveTileSettings->GetAt(i));
            });
          }
          return tsk;
        }
      });
		}
	};
}