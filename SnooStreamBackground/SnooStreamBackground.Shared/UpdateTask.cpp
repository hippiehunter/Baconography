#include "pch.h"
#include "SimpleRedditService.h"
#include "LockScreenSettings.h"
#include "LockScreenHistory.h"
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
      auto tileUpdateTask = RunTileUpdater(lockScreenSettings, lockScreenHistory).then([]()
			{

			});

      if (lockScreenSettings->LockScreenStyle != SnooStreamBackground::LockScreenStyle::Off)
			{
        tileUpdateTask.then([&]()
        {
          return RunLockscreenUpdater(lockScreenSettings, lockScreenHistory);
        }).then([&]()
				{
					deferral->Complete();
				});
			}
      else
			{
				tileUpdateTask.then([&]()
				{
					deferral->Complete();
				});
			}
		}
  public:
		UpdateBackgroundTask()
		{

		}
	private:
    task<void> RunTileUpdater(LockScreenSettings^ settings, LockScreenHistory^ history)
		{
      return task<void>();
		}


    task<void> RunLockscreenUpdater(LockScreenSettings^ settings, LockScreenHistory^ history)
		{
      return task<void>();
		}
	};
}