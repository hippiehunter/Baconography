#include "pch.h"

#include <time.h>
#include <fstream>
#include <chrono>

#include "ImageUtilities.h"

using namespace std;
using namespace Platform;
using namespace SnooStreamBackground;
using namespace concurrency;
using namespace Nokia::Graphics::Imaging;
using namespace Platform::Collections;
using namespace Windows::Storage::Streams;
using namespace Windows::Storage;
using namespace Windows::Security::Cryptography::Core;
using namespace Windows::Security::Cryptography;
using namespace Windows::Foundation;
using Windows::Web::Http::HttpClient;

chrono::duration<chrono::system_clock> toDuration(DateTime dt)
{
    return chrono::seconds(dt.UniversalTime / 10000000ULL - 11644473600ULL);
}

DateTime toFileTime(const chrono::duration<chrono::system_clock>& point)
{
    long long unixTime = std::chrono::duration_cast<std::chrono::seconds>(point).count();
    DateTime result;
    result.UniversalTime = unixTime * 10000000ULL + 11644473600ULL;
    return result;
}

task<void> ImageUtilities::ClearOldTempImages()
{
    auto oldFileQuery = ApplicationData::Current->TemporaryFolder->CreateFileQuery();
    return create_task(oldFileQuery->GetFilesAsync())
        .then([=](task<Windows::Foundation::Collections::IVectorView<StorageFile^>^> filesTask)
    {
        try
        {
            auto cutoff = chrono::system_clock::now().time_since_epoch() - chrono::hours(96);
            for (auto file : filesTask.get())
            {
                auto dateCreated = toDuration(file->DateCreated);
                if (dateCreated < cutoff)
                    create_task(file->DeleteAsync()).then([](task<void> deleteResult) { try { deleteResult.get(); } catch (...) {} });
            }
        }
        catch (...)
        {
        }
    });
}

task<IImageProvider^> ImageUtilities::GetImageSource(String^ url)
{
    auto httpClient = ref new HttpClient();
    return create_task(httpClient->GetBufferAsync(ref new Uri(url)))
        .then([=](task<IBuffer^> bufferTask) -> IImageProvider^
    {
        try
        {
            return ref new BufferImageSource(bufferTask.get());
        }
        catch (...)
        {
            return nullptr;
        }
    });
}

String^ ImageUtilities::ComputeMD5(String^ str)
{
    auto alg = HashAlgorithmProvider::OpenAlgorithm("MD5");
    auto buff = CryptographicBuffer::ConvertStringToBinary(str, BinaryStringEncoding::Utf8);
    auto hashed = alg->HashData(buff);
    auto res = CryptographicBuffer::EncodeToHexString(hashed);
    return res;
}

task<String^> ImageUtilities::MakeTileSizedImage(IImageProvider^ imageSource, String^ url, float height, float width)
{
    try
    {
        return create_task(imageSource->GetInfoAsync())
            .then([=](task<ImageProviderInfo^> infoTask)
        {
            try
            {
                auto imageSize = infoTask.get()->ImageSize;
                auto filter = ref new FilterEffect(imageSource);
                auto filters = ref new Vector<IFilter^>();

                auto frameScale = std::min(width / imageSize.Width, height / imageSize.Height);
                if (frameScale < 1.0)
                {
                    auto targetRatio = width / height;
                    auto currentRatio = imageSize.Width / imageSize.Height;
                    auto ratioDifference = std::abs(targetRatio - currentRatio);
                    if (targetRatio < currentRatio)
                    {
                        filters->Append(ref new ReframingFilter(Rect(0.0, 0.0, imageSize.Width * (targetRatio / currentRatio), imageSize.Height), 0.0));
                    }
                    else
                    {
                        filters->Append(ref new ReframingFilter(Rect(0.0, 0.0, imageSize.Width, imageSize.Height * (currentRatio / targetRatio)), 0.0));
                    }
                }

                filter->Filters = filters;
                auto render = ref new JpegRenderer(filter);
                render->Size = Size(width, height);
                render->Quality = 0.75;
                return create_task(render->RenderAsync())
                    .then([=](task<IBuffer^> jpegBufferTask)
                {
                    try
                    {
                        auto jpegBuffer = jpegBufferTask.get();
                        auto widthString = to_wstring(width);
                        auto heightString = to_wstring(height);
                        auto targetFileName = L"LiveTile" + ref new String(widthString.data(), widthString.size()) + "x" + 
                            ref new String(heightString.data(), heightString.size()) + ComputeMD5(url) + ".jpg";
                        return create_task(ApplicationData::Current->LocalFolder->CreateFileAsync(targetFileName, CreationCollisionOption::FailIfExists))
                            .then([=](task<StorageFile^> targetFileTask)
                        {
                            try
                            {
                                auto targetFile = targetFileTask.get();
                                return create_task(targetFile->OpenAsync(FileAccessMode::ReadWrite))
                                    .then([=](IRandomAccessStream^ targetStream)
                                {
                                    return create_task(targetStream->WriteAsync(jpegBuffer))
                                        .then([=](unsigned int bytesWriten)
                                    {
                                        return targetFile->DisplayName;
                                    });
                                });
                            }
                            catch (Exception^ ex)
                            {
                                return task_from_result<String^>(targetFileName);
                            }
                        });
                    }
                    catch (...)
                    {
                        return task_from_result<String^>(nullptr);
                    }
                });
            }
            catch (...)
            {
                return task_from_result<String^>(nullptr);
            }
        });
    }
    catch (...)
    {
        return task_from_result<String^>(nullptr);
    }
}

task<vector<ImageInfo^>> ImageUtilities::MakeLiveTileImages(vector<ImageInfo^> liveTileFiles, LockScreenHistory^ history, vector<tuple<String^, String^>> liveTileTpls, int targetCount, int targetIndex)
{
    if (targetIndex < liveTileTpls.size() && liveTileFiles.size() < targetCount)
    {
        auto targetUrl = get<1>(liveTileTpls[targetIndex]);
        auto targetTitle = get<0>(liveTileTpls[targetIndex]);
        wstring localPath(Windows::Storage::ApplicationData::Current->LocalFolder->Path->Data());
        auto widthString = to_wstring(150);
        auto heightString = to_wstring(310);
        auto wideFileName = L"LiveTile" + ref new String(widthString.data(), widthString.size()) + "x" +
            ref new String(heightString.data(), heightString.size()) + ComputeMD5(targetUrl) + ".jpg";

        auto squareFileName = L"LiveTile" + ref new String(widthString.data(), widthString.size()) + "x" +
            ref new String(widthString.data(), widthString.size()) + ComputeMD5(targetUrl) + ".jpg";

        auto madeImageInfo = ref new ImageInfo(targetUrl, squareFileName, nullptr, wideFileName, history->Age(targetUrl), targetTitle);

        wofstream settingsFile(localPath, std::ios_base::in | std::ios_base::binary);
        if (settingsFile.is_open())
        {
            settingsFile.close();
            return MakeLiveTileImages(liveTileFiles, history, liveTileTpls, targetCount, targetIndex + 1);
        }
        else
        {
            return GetImageSource(targetUrl)
                .then([=](IImageProvider^ imageSource)
            {
                if (imageSource != nullptr)
                {
                    return MakeTileSizedImage(imageSource, targetUrl, 150, 310)
                        .then([=](String^ filePath)
                    {
                        liveTileFiles[targetIndex]->LocalWideUrl = filePath;
                        return MakeTileSizedImage(imageSource, targetUrl, 150, 150)
                            .then([=](String^ smallFilePath)
                        {
                            liveTileFiles[targetIndex]->LocalSmallSquareUrl = smallFilePath;
                            return MakeLiveTileImages(liveTileFiles, history, liveTileTpls, targetCount, targetIndex + 1);
                        });
                    });
                }
                else
                    return MakeLiveTileImages(liveTileFiles, history, liveTileTpls, targetCount, targetIndex + 1);
            });
        }
    }
    else
        return concurrency::task_from_result(liveTileFiles);
}