#include "pch.h"
#include "SimpleRedditService.h"
#include "LockScreenSettings.h"
#include "LockScreenHistory.h"
#include "LockScreenViewModel.h"
#include "LockScreenViewControl.xaml.h"
#include "LockScreenItemView.xaml.h"
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
#ifdef WINDOWS_PHONE
		XamlRenderingBackgroundTask
#else
		public IBackgroundTask
#endif
	{
  private:
    std::unique_ptr<SimpleRedditService> redditService;
  public:
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
      NavRightGlyph = "\uE0AD";
      PhotoGlyph = "\uE114";
      VideoGlyph = "\uE116";
      WebGlyph = "\uE128";
      DetailsGlyph = "\uE14C";
		}
	private:
    // RenderTargetBitmap Pixel Data
    unsigned int pixelWidth;
    unsigned int pixelHeight;
    String^ NavRightGlyph;
    String^ PhotoGlyph;
    String^ VideoGlyph;
    String^ WebGlyph;
    String^ DetailsGlyph;

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

    String^ GetGlyph(String^ link)
    {
      try
      {
        String^ targetHost = "";
        
        Uri^ uri = nullptr;

        if (link == nullptr || link->Length() == 0)
          return DetailsGlyph;

        uri = ref new Uri(link);
        wstring filename(uri->Path->Data(), uri->Path->Length());
        targetHost = uri->Domain;

        if (targetHost == "youtube.com" ||
          targetHost == "liveleak.com")
          return VideoGlyph;

        if (targetHost == "imgur.com" ||
          targetHost == "min.us" ||
          targetHost == "quickmeme.com" ||
          targetHost == "qkme.me" ||
          targetHost == "memecrunch.com" ||
          targetHost == "flickr.com" ||
          ends_with(filename, L".jpg") ||
          ends_with(filename, L".gif") ||
          ends_with(filename, L".png") ||
          ends_with(filename, L".jpeg"))
          return PhotoGlyph;
      }
      catch(...) { }

      return WebGlyph;
    }

    template<typename T>
    void Shuffle(Windows::Foundation::Collections::IVector<T>^ list)
    {
      srand((unsigned int)time(nullptr));
      int n = list->Size;
      while (n > 1)
      {
        n--;
        int k = rand() % (n - 1);
        auto&& value = list->GetAt(k);
        list->SetAt(k, list->GetAt(n));
        list->SetAt(n, value);
      }
    }

    bool Toast(Platform::String^ message)
    {
      auto toastNotifier = Windows::UI::Notifications::ToastNotificationManager::CreateToastNotifier();
      ToastTemplateType toastTemplate = ToastTemplateType::ToastImageAndText01;
      XmlDocument^ toastXml = ToastNotificationManager::GetTemplateContent(toastTemplate);

      XmlNodeList^ toastTextElements = toastXml->GetElementsByTagName("text");
      toastTextElements->Item(0)->InnerText = message;

      XmlNodeList^ toastImageAttributes = toastXml->GetElementsByTagName("image");
      static_cast<XmlElement^>(toastImageAttributes->Item(0))->SetAttribute("src", "ms-appx:///assets/redWide.png");
      static_cast<XmlElement^>(toastImageAttributes->Item(0))->SetAttribute("alt", "red graphic");

      IXmlNode^ toastNode = toastXml->SelectSingleNode("/toast");
      static_cast<XmlElement^>(toastNode)->SetAttribute("launch", "{\"type\":\"toast\",\"param1\":\"12345\",\"param2\":\"67890\"}");

      auto toastNotification = ref new Windows::UI::Notifications::ToastNotification(toastXml);
      toastNotifier->Show(toastNotification);
      return true;
    }

    task<void> RenderAndSaveToFileAsync(UIElement^ uiElement, String^ outputImageFilename, uint32 width, uint32 height)
    {
      RenderTargetBitmap^ rtb = ref new RenderTargetBitmap();
      return create_task(rtb->RenderAsync(uiElement, width, height))
        .then([this, rtb]() -> IAsyncOperation<IBuffer^>^
      {
        this->pixelWidth = (uint32)rtb->PixelWidth;
        this->pixelHeight = (uint32)rtb->PixelHeight;
        return rtb->GetPixelsAsync();
      }).then([this, rtb, outputImageFilename](IBuffer^ buffer)
      {
        return WriteBufferToFile(outputImageFilename, buffer);
      });
    }

    Array<unsigned char>^ GetArrayFromBuffer(IBuffer^ buffer)
    {
      auto dataReader = Windows::Storage::Streams::DataReader::FromBuffer(buffer);
      Array<unsigned char>^ data = ref new Array<unsigned char>(buffer->Length);
      dataReader->ReadBytes(data);
      return data;
    }

    task<void> WriteBufferToFile(String^ outputImageFilename, IBuffer^ buffer)
    {
      auto resultStorageFolder = Windows::ApplicationModel::Package::Current->InstalledLocation;

      return create_task(resultStorageFolder->CreateFileAsync(outputImageFilename, CreationCollisionOption::ReplaceExisting)).
        then([](StorageFile^ outputStorageFile)
      {
        return outputStorageFile->OpenAsync(FileAccessMode::ReadWrite);
      }).then([](IRandomAccessStream^ outputFileStream)
      {
        return BitmapEncoder::CreateAsync(BitmapEncoder::PngEncoderId, outputFileStream);
      }).then([this, buffer](BitmapEncoder^ encoder)
      {
        encoder->SetPixelData(BitmapPixelFormat::Bgra8, BitmapAlphaMode::Premultiplied, this->pixelWidth, this->pixelHeight, 96, 96, GetArrayFromBuffer(buffer));
        return encoder->FlushAsync();
      });
    }

    // Send a tile notification with the new tile payload. 
    void UpdateTile(String^ tileUpdateImagePath)
    {
      auto tileUpdater = TileUpdateManager::CreateTileUpdaterForApplication();
      tileUpdater->Clear();
      auto tileTemplate = TileUpdateManager::GetTemplateContent(TileTemplateType::TileSquare150x150Image);
      auto tileImageAttributes = tileTemplate->GetElementsByTagName("image");
      static_cast<XmlElement^>(tileImageAttributes->Item(0))->SetAttribute("src", tileUpdateImagePath);
      auto notification = ref new TileNotification(tileTemplate);
      tileUpdater->Update(notification);
    }

    task<void> UpdateLockScreen(String^ lockScreenImagePath)
    {
#ifdef WINDOWS_PHONE
      return task<void>();
#else
      return create_task(Windows::Storage::StorageFile::GetFileFromPathAsync(lockScreenImagePath)).then([](StorageFile^ file)
      {
        return Windows::System::UserProfile::LockScreen::SetImageFileAsync(file);
      });
#endif 
    }

    task<void> RunPrimaryTileUpdater(LockScreenSettings^ settings, LockScreenHistory^ history, LiveTileSettings^ liveTile)
    {
      return redditService->GetPostsBySubreddit(settings->LockScreenOverlayItemsReddit, 100)
        .then([=](std::vector<tuple<String^, String^>> messages)
      {
        vector<String^> liveTileImageUrls;
        for (auto& message : messages)
        {
          std::wstring url(std::get<1>(message)->Begin(), std::get<1>(message)->End());
          if(ends_with(url, L".jpg") ||
            ends_with(url, L".jpeg") ||
            ends_with(url, L".png"))
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
      
      lockScreenViewModel->ImageSource = "lockScreenCache1.jpg";
      lockScreenViewModel->OverlayOpacity = settings->LockScreenOverlayOpacity / 100.0f;
      lockScreenViewModel->NumberOfItems = settings->LockScreenOverlayItemsCount;
      lockScreenViewModel->RoundedCorners = settings->LockScreenOverlayRoundedEdges;
      lockScreenViewModel->OverlayItems = ref new Platform::Collections::Vector<LockScreenMessage^>();

      return redditService->GetNewMessages().then([=](vector<String^> messages)
      {
        if (messages.size() > 0)
        {
          for (auto&& message : messages)
          {
            if (Toast(message))
            {
              lockScreenViewModel->OverlayItems->Append(ref new LockScreenMessage(message, "\uE119"));
            }
          }
        }
      }).then([=]()
      {
        return redditService->GetPostsBySubreddit(settings->LockScreenOverlayItemsReddit, 10);
      }).then([=](std::vector<tuple<String^, String^>> redditItems)
      {
        for (auto&& message : redditItems)
        {
          if (lockScreenViewModel->OverlayItems->Size > (settings->LockScreenOverlayItemsCount - 1))
            break;

          lockScreenViewModel->OverlayItems->Append(ref new LockScreenMessage(std::get<0>(message), GetGlyph(std::get<1>(message))));
        }

        Shuffle(history->LockScreenImages);
        if (history->LockScreenImages->Size > 0)
        {
          lockScreenViewModel->ImageSource = history->LockScreenImages->GetAt(0)->LocalUrl;
        }

        auto lockScreenControl = ref new LockScreenViewControl(lockScreenViewModel);
        return RenderAndSaveToFileAsync(lockScreenControl, Windows::Storage::ApplicationData::Current->LocalFolder->Path + "\\lockscreen.jpg", 480, 800);
      }).then([=]()
      {
        return UpdateLockScreen(Windows::Storage::ApplicationData::Current->LocalFolder->Path + "\\lockscreen.jpg");
      }).then([=]()
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