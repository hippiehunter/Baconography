#include "pch.h"
#include "SimpleRedditService.h"
#include "LockScreenSettings.h"
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

	private:
		vector<tuple<String^, String^>> currentTileImages;
		vector<tuple<String^, String^>> tileHistory;
		String^ lastLockScreenUpdate;
		vector<String^> lockScreenImages;

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
			auto tileUpdateTask = RunTileUpdater().then([]()
			{

			});

			if (lastLockScreenUpdate)
			{
        tileUpdateTask.then([&]()
        {
          return RunLockscreenUpdater();
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
		task<void> RunTileUpdater()
		{

		}


		task<void> RunLockscreenUpdater()
		{

		}
	};
}