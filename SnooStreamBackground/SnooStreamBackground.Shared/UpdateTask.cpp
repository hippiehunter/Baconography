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

        void RunExternal()
        {
#ifdef WINDOWS_PHONE
            OnRun(nullptr);
#else
            Run(nullptr);
#endif
        }

#ifdef WINDOWS_PHONE
    protected:
        void OnRun(IBackgroundTaskInstance^ taskInstance) override
#else
    public:
        virtual void Run(IBackgroundTaskInstance^ taskInstance)
#endif
        {
            Platform::Agile<Windows::ApplicationModel::Background::BackgroundTaskDeferral> deferral;
            if (taskInstance != nullptr)
            {
                deferral = Platform::Agile<Windows::ApplicationModel::Background::BackgroundTaskDeferral>(taskInstance->GetDeferral());
            }
            auto lockScreenSettings = ref new LockScreenSettings();
            auto lockScreenHistory = ref new LockScreenHistory();

            redditService = std::make_unique<SimpleRedditService>(RedditOAuth::Deserialize(lockScreenSettings->RedditOAuth));

            auto tileUpdateTask = RunTileUpdater(lockScreenSettings, lockScreenHistory).then([=]()
            {
                if (taskInstance != nullptr)
                {
                    deferral->Complete();
                }
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
            catch (...) {}

            return WebGlyph;
        }

        template<typename T>
        void Shuffle(Windows::Foundation::Collections::IVector<T>^ list)
        {
            srand((unsigned int) time(nullptr));
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
                this->pixelWidth = (uint32) rtb->PixelWidth;
                this->pixelHeight = (uint32) rtb->PixelHeight;
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

        task<Nokia::Graphics::Imaging::BufferImageSource^> GetImageSource(Platform::String^ url)
        {
            auto httpClient = ref new Windows::Web::Http::HttpClient();
            return create_task(httpClient->GetBufferAsync(ref new Windows::Foundation::Uri(url)))
                .then([=](task<Windows::Storage::Streams::IBuffer^> bufferTask)
            {
                try
                {
                    return ref new Nokia::Graphics::Imaging::BufferImageSource(bufferTask.get());
                }
                catch (...)
                {
                    return (Nokia::Graphics::Imaging::BufferImageSource^)nullptr;
                }
            });
        }

        Platform::String^ ComputeMD5(Platform::String^ str)
        {
            auto alg = Windows::Security::Cryptography::Core::HashAlgorithmProvider::OpenAlgorithm("MD5");
            auto buff = Windows::Security::Cryptography::CryptographicBuffer::ConvertStringToBinary(str, Windows::Security::Cryptography::BinaryStringEncoding::Utf8);
            auto hashed = alg->HashData(buff);
            auto res = Windows::Security::Cryptography::CryptographicBuffer::EncodeToHexString(hashed);
            return res;
        }

        task<Platform::String^> MakeTileSizedImage(Nokia::Graphics::Imaging::BufferImageSource^ imageSource, Platform::String^ url, float height, float width)
        {
            try
            {
                return create_task(imageSource->GetInfoAsync())
                    .then([=](task<Nokia::Graphics::Imaging::ImageProviderInfo^> infoTask)
                {
                    try
                    {
                        auto imageSize = infoTask.get()->ImageSize;
                        auto filter = ref new Nokia::Graphics::Imaging::FilterEffect(imageSource);
                        auto filters = ref new Platform::Collections::Vector<Nokia::Graphics::Imaging::IFilter^>();

                        auto frameScale = std::min(width / imageSize.Width, height / imageSize.Height);
                        if (frameScale < 1.0)
                        {
                            auto targetRatio = width / height;
                            auto currentRatio = imageSize.Width / imageSize.Height;
                            auto ratioDifference = std::abs(targetRatio - currentRatio);
                            if (targetRatio < currentRatio)
                            {
                                filters->Append(ref new Nokia::Graphics::Imaging::ReframingFilter(Windows::Foundation::Rect(0.0, 0.0, imageSize.Width * (targetRatio / currentRatio), imageSize.Height), 0.0));
                            }
                            else
                            {
                                filters->Append(ref new Nokia::Graphics::Imaging::ReframingFilter(Windows::Foundation::Rect(0.0, 0.0, imageSize.Width, imageSize.Height * (currentRatio / targetRatio)), 0.0));
                            }
                        }

                        filter->Filters = filters;
                        auto render = ref new Nokia::Graphics::Imaging::JpegRenderer(filter);
                        render->Size = Windows::Foundation::Size(width, height);
                        render->Quality = 0.75;
                        return create_task(render->RenderAsync())
                            .then([=](task<Windows::Storage::Streams::IBuffer^> jpegBufferTask)
                        {
                            try
                            {
                                auto jpegBuffer = jpegBufferTask.get();
                                auto targetFileName = L"LiveTile" + ComputeMD5(url) + ".jpg";
                                return create_task(Windows::Storage::ApplicationData::Current->LocalFolder->CreateFileAsync(targetFileName, Windows::Storage::CreationCollisionOption::FailIfExists))
                                    .then([=](task<Windows::Storage::StorageFile^> targetFileTask)
                                {
                                    try
                                    {
                                        auto targetFile = targetFileTask.get();
                                        return create_task(targetFile->OpenAsync(Windows::Storage::FileAccessMode::ReadWrite))
                                            .then([=](Windows::Storage::Streams::IRandomAccessStream^ targetStream)
                                        {
                                            return create_task(targetStream->WriteAsync(jpegBuffer))
                                                .then([=](unsigned int bytesWriten)
                                            {
                                                return targetFile->DisplayName;
                                            });
                                        });
                                    }
                                    catch (Platform::Exception^ ex)
                                    {
                                        return concurrency::task_from_result<Platform::String^>(targetFileName);
                                    }
                                });
                            }
                            catch (...)
                            {
                                return concurrency::task_from_result<Platform::String^>(nullptr);
                            }
                        });
                    }
                    catch (...)
                    {
                        return concurrency::task_from_result<Platform::String^>(nullptr);
                    }
                });
            }
            catch (...)
            {
                return concurrency::task_from_result<Platform::String^>(nullptr);
            }
        }

        task<vector<String^>> MakeLiveTileImages(vector<String^> liveTileFiles, vector<String^> liveTileUrls, float height, float width, int targetIndex = 0)
        {
            if (targetIndex < liveTileUrls.size())
            {
                return GetImageSource(liveTileUrls[targetIndex])
                    .then([=](Nokia::Graphics::Imaging::BufferImageSource^ imageSource)
                {
                    if (imageSource != nullptr)
                    {
                        return MakeTileSizedImage(imageSource, liveTileUrls[targetIndex], height, width)
                            .then([=](Platform::String^ filePath)
                        {
                            vector<String^> nextLiveTileFiles(liveTileFiles.begin(), liveTileFiles.end());
                            nextLiveTileFiles.push_back(filePath);
                            return MakeLiveTileImages(nextLiveTileFiles, liveTileUrls, height, width, targetIndex + 1);
                        });
                    }
                    else
                        return MakeLiveTileImages(liveTileFiles, liveTileUrls, height, width, targetIndex + 1);
                });
            }
            else
                return concurrency::task_from_result(liveTileFiles);
        }

        task<void> RunPrimaryTileUpdater(LockScreenSettings^ settings, LockScreenHistory^ history, LiveTileSettings^ liveTile)
        {
            return redditService->GetPostsBySubreddit(liveTile->LiveTileItemsReddit, 100)
                .then([=](std::vector<tuple<String^, String^>> messages)
            {
                vector<String^> liveTileImageUrls;
                for (auto& message : messages)
                {
                    std::wstring url(std::get<1>(message)->Begin(), std::get<1>(message)->End());
                    if (ends_with(url, L".jpg") ||
                        ends_with(url, L".jpeg") ||
                        ends_with(url, L".png"))
                    {
                        liveTileImageUrls.push_back(std::get<1>(message));
                        if (liveTileImageUrls.size() > 20)
                            break;
                    }
                }
                //TODO
                //check history to see if we've already shown this tile in the past if so, penalize it and prefer other tiles
                //need to do cleanup on LockScreen*.jpg files with older creation dates, after a sucessfully building the live tiles
                return create_task(MakeLiveTileImages(vector < String^ > {}, liveTileImageUrls, 150, 310))
                    .then([=](vector<String^> liveTileImageUrls)
                {
                    auto tileUpdater = Windows::UI::Notifications::TileUpdateManager::CreateTileUpdaterForApplication();
                    tileUpdater->EnableNotificationQueue(true);
                    tileUpdater->Clear();
                    int tagId = 0;
                    for (auto&& liveTileImageUrl : liveTileImageUrls)
                    {
                        auto tileTemplate = Windows::UI::Notifications::TileUpdateManager::GetTemplateContent(Windows::UI::Notifications::TileTemplateType::TileWide310x150Image);
                        auto tileAttributes = tileTemplate->GetElementsByTagName("image");
                        tileAttributes->GetAt(0)->Attributes->GetNamedItem("src")->InnerText = "ms-appdata:///local/" + liveTileImageUrl;
                        auto tile = ref new Windows::UI::Notifications::TileNotification(tileTemplate);
                        auto tagString = std::to_wstring(tagId);
                        tile->Tag = ref new Platform::String(tagString.c_str(), tagString.size());
                        tileUpdater->Update(tile);
                        if (tagId++ > 4)
                            break;

                    }

                });
            });
        }

        task<void> RunSecondaryTileUpdater(LockScreenSettings^ settings, LockScreenHistory^ history, LiveTileSettings^ liveTile)
        {
            return task<void>();
        }

        task<void> RunTileUpdater(LockScreenSettings^ settings, LockScreenHistory^ history)
        {
            /*LockScreenViewModel^ lockScreenViewModel = ref new LockScreenViewModel();

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
            {*/
            if (settings->LiveTileSettings != nullptr && settings->LiveTileSettings->Size == 1)
                return RunPrimaryTileUpdater(settings, history, settings->LiveTileSettings->GetAt(0));
            else if (settings->LiveTileSettings != nullptr)
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
            else
                return task<void>();
            //});
        }
    };
}