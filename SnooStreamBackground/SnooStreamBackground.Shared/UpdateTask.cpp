#include "pch.h"
#include "SimpleRedditService.h"
#include "LockScreenSettings.h"
#include "LockScreenHistory.h"
#include "LockScreenViewModel.h"
#include "LockScreenViewControl.xaml.h"
#include "LockScreenItemView.xaml.h"
#include "ImageUtilities.h"
#include "NetworkUtilities.h"
#include "LiveTileUtilities.h"
#include "ActivityManager.h"
#include <vector>
#include <tuple>
#include <iostream>
#include <fstream>
#include <string>

using namespace Windows::ApplicationModel::Background;
using namespace Windows::System::Threading;
using namespace Windows::UI::Xaml::Media::Imaging;
using namespace Windows::UI::Notifications;
using namespace Windows::UI::Xaml;
using namespace Windows::UI::Xaml::Controls;
using namespace Windows::UI::Xaml::Markup;
using namespace Windows::UI::Xaml::Media;
using namespace Windows::Graphics::Display;
using namespace Windows::Graphics::Imaging;
using namespace Windows::Storage::Streams;
using namespace Windows::Data::Xml::Dom;
using namespace Windows::Globalization;
using namespace Windows::Globalization::DateTimeFormatting;
using namespace Windows::UI;
using namespace Windows::Storage;
using Windows::Foundation::Uri;
using Windows::Foundation::IAsyncOperation;
using concurrency::task;
using concurrency::create_task;
using Platform::String;
using Platform::Array;
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
        public IBackgroundTask
    {
    private:
        std::unique_ptr<SimpleRedditService> redditService;
		bool _external;
    public:

        void RunExternal()
        {
			_external = true;
            Run(nullptr);
        }
    public:
        virtual void Run(IBackgroundTaskInstance^ taskInstance)
        {
            Platform::Agile<Windows::ApplicationModel::Background::BackgroundTaskDeferral> deferral;
            if (taskInstance != nullptr)
            {
                deferral = Platform::Agile<Windows::ApplicationModel::Background::BackgroundTaskDeferral>(taskInstance->GetDeferral());
            }
            auto lockScreenSettings = ref new LockScreenSettings();
            auto lockScreenHistory = ref new LockScreenHistory();

            redditService = std::make_unique<SimpleRedditService>(RedditOAuth::Deserialize(lockScreenSettings->RedditOAuth));

            auto tileUpdateTask = RunTileUpdater(lockScreenSettings, lockScreenHistory)
                .then([=](task<void> task)
            {
                return ImageUtilities::ClearOldTempImages();
            })
                .then([=](task<void> task)
            {
                lockScreenHistory->Store();
                if (taskInstance != nullptr)
                {
                    deferral->Complete();
                }
            });
        }
    private:

        bool ends_with(std::wstring const &fullString, std::wstring const &ending)
        {
            if (fullString.length() >= ending.length())
            {
                return (0 == fullString.compare(fullString.length() - ending.length(), ending.length(), ending));
            }
            else
            {
                return false;
            }
        }

        task<void> UpdateLockScreen(String^ lockScreenImagePath)
        {
#ifdef WINDOWS_PHONE
            return concurrency::task_from_result();
#else
            return create_task(Windows::Storage::StorageFile::GetFileFromPathAsync(lockScreenImagePath)).then([](StorageFile^ file)
            {
                return Windows::System::UserProfile::LockScreen::SetImageFileAsync(file);
            });
#endif 
        }

        int GetTileCountTarget(bool isPrimary)
        {
            if (isPrimary)
            {
                if (!NetworkUtilities::IsHighPriorityNetworkOk())
                    return 0;
                else if (NetworkUtilities::LowPriorityNetworkOk())
                    return 20;
                else
                    return 5;
            }
            else
            {
                if (!NetworkUtilities::IsHighPriorityNetworkOk())
                    return 0;
                else if (NetworkUtilities::LowPriorityNetworkOk())
                    return 5;
                else
                    return 0;
            }
        }

        task<void> RunPrimaryTileUpdater(LockScreenSettings^ settings, LockScreenHistory^ history, LiveTileSettings^ liveTile)
        {
            return redditService->GetPostsBySubreddit(liveTile->LiveTileItemsReddit, 100)
                .then([=](task<std::vector<tuple<String^, String^>>> messagesTask)
            {
				try
				{
					auto messages = messagesTask.get();
					std::vector<tuple<String^, String^>> liveTileImageUrls;
					for (auto& message : messages)
					{
						std::wstring url(std::get<1>(message)->Begin(), std::get<1>(message)->End());
						if (ends_with(url, L".jpg") ||
							ends_with(url, L".jpeg") ||
							ends_with(url, L".png"))
						{
							liveTileImageUrls.push_back(message);
						}
					}

					//check history to see if we've already shown this tile in the past if so, penalize it and prefer other tiles
					std::sort(liveTileImageUrls.begin(), liveTileImageUrls.end(), [&](tuple<String^, String^> option1, tuple<String^, String^> option2)
					{
						return history->Age(std::get<1>(option1)) > history->Age(std::get<1>(option2));
					});

					return create_task(ImageUtilities::MakeLiveTileImages(vector<ImageInfo^> {}, history, liveTileImageUrls, GetTileCountTarget(true)))
						.then([=](vector<ImageInfo^> liveTileImageUrls)
					{
						if (liveTileImageUrls.size() > 0)
						{
							auto tileUpdater = Windows::UI::Notifications::TileUpdateManager::CreateTileUpdaterForApplication();
							history->CurrentTileImages = ref new Platform::Collections::Vector<ImageInfo^>(liveTileImageUrls);
							LiveTileUtilities::MakeLiveTile(history, liveTile, liveTileImageUrls, tileUpdater);
						}
					});
				}
				catch (...)
				{
					return concurrency::task_from_result();
				}
            });
        }

        task<void> RunSecondaryTileUpdater(LockScreenSettings^ settings, LockScreenHistory^ history, LiveTileSettings^ liveTile)
        {
			return concurrency::task_from_result();
        }

        task<void> RunTileUpdater(LockScreenSettings^ settings, LockScreenHistory^ history)
        {
            auto activityManager = ref new ActivityManager();
            
            auto runTilePart = [=]()
            {

                if (settings->LiveTileSettings != nullptr && settings->LiveTileSettings->Size == 1)
                    return RunPrimaryTileUpdater(settings, history, settings->LiveTileSettings->GetAt(0));
                else if (settings->LiveTileSettings != nullptr)
                {
                    auto tsk = RunPrimaryTileUpdater(settings, history, settings->LiveTileSettings->GetAt(0));
                    for (int i = 1; i < settings->LiveTileSettings->Size; i++)
                    {
                        tsk = tsk.then([&](task<void> priorTask)
                        {
                            return RunSecondaryTileUpdater(settings, history, settings->LiveTileSettings->GetAt(i));
                        });
                    }
                    return tsk;
                }
                else
                    return concurrency::task_from_result();
            };
            
            if (!_external && activityManager->NeedsRefresh)
            {
                return create_task(activityManager->Refresh(settings->RedditOAuth, nullptr))
                    .then([=]()
                {
                    activityManager->StoreState();
                    return runTilePart();
                });
            }
            else
                return runTilePart();
        }
    };
}