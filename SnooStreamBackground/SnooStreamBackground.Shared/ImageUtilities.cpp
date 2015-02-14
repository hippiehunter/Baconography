#include "pch.h"

#include <time.h>
#include <fstream>
#include <chrono>

#include "ImageUtilities.h"

using namespace std;
using namespace Platform;
using namespace SnooStreamBackground;
using namespace concurrency;
using namespace Lumia::Imaging;
using namespace Platform::Collections;
using namespace Windows::Storage::Streams;
using namespace Windows::Storage;
using namespace Windows::Security::Cryptography::Core;
using namespace Windows::Security::Cryptography;
using namespace Windows::Foundation;
using Windows::Web::Http::HttpClient;

chrono::seconds toDuration(DateTime dt)
{
    return chrono::seconds((dt.UniversalTime - 116444736000000000LL) / 10000000ULL);
}

bool starts_with(std::wstring const &fullString, std::wstring const &start)
{
    if (fullString.length() >= start.length())
    {
        return (0 == fullString.compare(0, start.length(), start));
    }
    else
    {
        return false;
    }
}

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

bool ImageUtilities::FileExists(Platform::String^ fileName)
{
	_stat64i32 stat;
	return _wstat(fileName->Data(), &stat) == 0;
}

task<void> ImageUtilities::ClearOldTempImages()
{
    return create_task(ApplicationData::Current->TemporaryFolder->GetFilesAsync())
        .then([=](task<Windows::Foundation::Collections::IVectorView<StorageFile^>^> filesTask)
    {
        try
        {
            auto cutoff = chrono::duration_cast<chrono::seconds>(chrono::system_clock::now().time_since_epoch() - chrono::hours(96));
            for (auto file : filesTask.get())
            {
                auto dateCreated = toDuration(file->DateCreated);
                if (dateCreated < cutoff || starts_with(wstring(file->Name->Data()), L"deleteme"))
                    create_task(file->DeleteAsync()).then([](task<void> deleteResult) { try { deleteResult.get(); } catch (...) {} });
            }

            //clean up the live tiles
            return create_task(ApplicationData::Current->LocalFolder->GetFilesAsync())
                .then([=](task<Windows::Foundation::Collections::IVectorView<StorageFile^>^> filesTask)
            {
				try
				{
					for (auto file : filesTask.get())
					{
						auto fileName = wstring(file->Name->Data());
						if (ends_with(fileName, L".jpg") && starts_with(fileName, L"LiveTile"))
						{
							auto dateCreated = toDuration(file->DateCreated);
							if (dateCreated < cutoff)
								create_task(file->DeleteAsync()).then([](task<void> deleteResult) { try { deleteResult.get(); } catch (...) {} });
						}
					}
				}
				catch (...) { }
				return task_from_result();
            });
        }
        catch (...)
        {
        }
		return task_from_result();
    });
}

task<BufferImageSource^> ImageUtilities::GetImageSource(String^ url)
{
    auto httpClient = ref new HttpClient();
    return create_task(httpClient->GetBufferAsync(ref new Uri(url)))
        .then([=](task<IBuffer^> bufferTask) -> BufferImageSource^
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

Platform::String^ ImageUtilities::MakeTempFileName(Platform::String^ prefix, Platform::String^ url, int height, int width)
{
    auto widthString = to_wstring((int) width);
    auto heightString = to_wstring((int) height);
    return prefix + ref new String(widthString.data(), widthString.size()) + "x" +
        ref new String(heightString.data(), heightString.size()) + ComputeMD5(url) + ".jpg";
}

task<String^> ImageUtilities::MakeTileSizedImage(BufferImageSource^ imageSource, String^ targetFileName, float height, float width, Windows::Storage::StorageFolder^ targetFolder)
{
    _stat64i32 stat;
    if (_wstat((targetFolder->Path + L"\\" + targetFileName)->Data(), &stat) == 0)
    {

        return task_from_result<String^>(targetFileName);
    }
    else
    {
        return create_task(targetFolder->CreateFileAsync(targetFileName, CreationCollisionOption::FailIfExists))
            .then([=](task<StorageFile^> targetFileTask)
        {
            auto cleanupTargetFile = [=]()
            {
                try
                {
                    create_task(targetFileTask.get()->DeleteAsync()).then([=](task<void> tsk)
                    {
                        try
                        {
                            tsk.get();
                        }
                        catch (...) {}
                    });
                }
                catch (...) {}
            };

            try
            {
                auto targetFile = targetFileTask.get();
                return create_task(targetFile->OpenAsync(FileAccessMode::ReadWrite))
                    .then([=](IRandomAccessStream^ targetStream)
                {
                    return create_task(imageSource->GetInfoAsync())
                        .then([=](task<ImageProviderInfo^> infoTask)
                    {
                        try
                        {
                            auto providerInfo = infoTask.get();
                            auto imageSize = providerInfo->ImageSize;
                            
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
                                    filters->Append(ref new Lumia::Imaging::Transforms::ReframingFilter(Rect(0.0, 0.0, imageSize.Width * (targetRatio / currentRatio), imageSize.Height), 0.0));
                                }
                                else
                                {
                                    filters->Append(ref new Lumia::Imaging::Transforms::ReframingFilter(Rect(0.0, 0.0, imageSize.Width, imageSize.Height * (currentRatio / targetRatio)), 0.0));
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
                                    return create_task(targetStream->WriteAsync(jpegBuffer))
                                        .then([=](unsigned int bytesWriten)
                                    {
                                        return targetFolder->Path + L"\\" + targetFile->DisplayName;
                                    });
                                }
                                catch (Platform::Exception^ ex)
                                {
                                    cleanupTargetFile();
                                    return task_from_exception<String^>(ex);
                                }
                            });

                        }
                        catch (Platform::Exception^ ex)
                        {
                            cleanupTargetFile();
                            return task_from_exception<String^>(ex);
                        }
                    });
                });
            }
            catch (Platform::Exception^ ex)
            {
                return task_from_result<String^>(targetFolder->Path + L"\\" + targetFileName);
            }
        });


    }
}

task<vector<ImageInfo^>> ImageUtilities::MakeLiveTileImages(vector<ImageInfo^> liveTileFiles, LockScreenHistory^ history, vector<tuple<String^, String^>> liveTileTpls, int targetCount, int targetIndex)
{
	if (liveTileFiles.size() > 0 && liveTileFiles.back()->Faulted)
		liveTileFiles.pop_back();

    if (targetIndex < liveTileTpls.size() && liveTileFiles.size() < targetCount)
    {
        auto targetUrl = get<1>(liveTileTpls[targetIndex]);
        auto targetTitle = get<0>(liveTileTpls[targetIndex]);
        wstring localPath(Windows::Storage::ApplicationData::Current->LocalFolder->Path->Data());
        auto wideFileName = MakeTempFileName(L"LiveTile", targetUrl, 310, 150);
        auto squareFileName = MakeTempFileName(L"LiveTile", targetUrl, 150, 150);

        auto madeImageInfo = ref new ImageInfo(targetUrl, squareFileName, nullptr, wideFileName, history->Age(targetUrl), targetTitle);
        liveTileFiles.push_back(madeImageInfo);
        auto wideFilePath = localPath + L"\\" + wstring(wideFileName->Data());
        auto squareFilePath = localPath + L"\\" + wstring(squareFileName->Data());
        _stat64i32 stat;
        if (_wstat(wideFilePath.c_str(), &stat) == 0 && _wstat(squareFilePath.c_str(), &stat) == 0)
        {
            return MakeLiveTileImages(liveTileFiles, history, liveTileTpls, targetCount, targetIndex + 1);
        }
        else
        {
            return GetImageSource(targetUrl)
                .then([=](task<BufferImageSource^> imageSourceTask)
            {
				try
				{
					auto imageSource = imageSourceTask.get();
					if (imageSource != nullptr)
					{
                        return MakeTileSizedImage(imageSource, wideFileName, 150, 310, Windows::Storage::ApplicationData::Current->LocalFolder)
							.then([=](task<String^> filePath)
						{
                            return MakeTileSizedImage(imageSource, squareFileName, 150, 150, Windows::Storage::ApplicationData::Current->LocalFolder)
								.then([=](task<String^> smallFilePath)
							{
                                try
                                {
                                    if (filePath.get() == nullptr || smallFilePath.get() == nullptr)
                                        madeImageInfo->Faulted = true;
                                }
                                catch (...){};
								
								
								return MakeLiveTileImages(liveTileFiles, history, liveTileTpls, targetCount, targetIndex + 1);
							});
						});
					}
					else
					{
						madeImageInfo->Faulted = true;
						return MakeLiveTileImages(liveTileFiles, history, liveTileTpls, targetCount, targetIndex + 1);
					}
				}
				catch (...)
				{
					madeImageInfo->Faulted = true;
					return concurrency::task_from_result(liveTileFiles);
				}
            });
        }
    }
    else
        return concurrency::task_from_result(liveTileFiles);
}


IAsyncOperation<Platform::String^>^ ImageUtilities::MakeSizedImage(Platform::String^ onDiskPrefix, Platform::String^ url, float height, float width)
{
    return create_async([=](cancellation_token token)
    {
        auto resultFileName = MakeTempFileName(onDiskPrefix, url, (int) height, (int) width);
        auto fullPath = Windows::Storage::ApplicationData::Current->TemporaryFolder->Path + L"\\" + resultFileName;
        if (FileExists(fullPath))
        {
            return task_from_result(fullPath);
        }
        else
        {
            auto httpClient = ref new HttpClient();
            return create_task(httpClient->GetAsync(ref new Uri(url), Windows::Web::Http::HttpCompletionOption::ResponseHeadersRead), token)
                .then([=](task<Windows::Web::Http::HttpResponseMessage^> responseTask)
            {
                try
                {
                    auto response = responseTask.get();
                    response->EnsureSuccessStatusCode();
                    auto contentLength = response->Content->Headers->ContentLength;
                    if (contentLength != nullptr && contentLength->Value > 1024 * 1024 * 4)
                    {
                        return task_from_exception<IBuffer^>(ref new Platform::OperationCanceledException("object size too large"));
                    }
                    return create_task(response->Content->ReadAsBufferAsync(), token);
                }
                catch (Platform::Exception^ ex)
                {
                    return task_from_exception<IBuffer^>(ex);
                }
            }, token)
                .then([=](task<IBuffer^> bufferTask)
            {
                try
                {
                    return MakeTileSizedImage(ref new BufferImageSource(bufferTask.get()), resultFileName, height, width, Windows::Storage::ApplicationData::Current->TemporaryFolder);
                }
                catch (Platform::Exception^ ex)
                {
                    return task_from_exception<Platform::String^>(ex);
                }
            }, token);
        }
    });
}

Platform::String^ ImageUtilities::TrySizedImage(Platform::String^ onDiskPrefix, Platform::String^ url, float height, float width)
{
    auto resultFileName = MakeTempFileName(onDiskPrefix, url, (int) height, (int) width);
    if (FileExists(resultFileName))
    {
        return resultFileName;
    }
    else
    {
        return nullptr;
    }
}