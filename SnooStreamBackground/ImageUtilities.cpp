#include "pch.h"

#include <time.h>
#include <fstream>
#include <chrono>
#include <wrl.h>
#include <wrl\client.h>
#include <wincodec.h>
#include <shcore.h>

#include "ImageUtilities.h"
#include "ResourceLoader.h"
#include "task_helper.h"

using namespace Microsoft::WRL;
using namespace std;
using namespace Platform;
using namespace SnooStreamBackground;
using namespace concurrency;
using namespace Platform::Collections;
using namespace Windows::Storage::Streams;
using namespace Windows::Storage;
using namespace Windows::Security::Cryptography::Core;
using namespace Windows::Security::Cryptography;
using namespace Windows::Foundation;
using namespace task_helper;
using Windows::Web::Http::HttpClient;

bool ImageUtilities::FileExists(Platform::String^ fileName)
{
	struct _stat64i32 statVar;
	return _wstat(fileName->Data(), &statVar) == 0;
}

task<void> ImageUtilities::ClearOldTempImages(concurrency::cancellation_token cancelToken)
{
	return ResourceLoader::CleanOldTemps();
}

task<IRandomAccessStream^> ImageUtilities::GetImageSource(String^ url, concurrency::cancellation_token cancelToken)
{
	return ResourceLoader::GetResource(url, [](auto buffer, auto expectedSize)
	{
		return make_tuple(true, true);
	}, [](auto buffer, auto finished, auto expectedSize)
	{
	}, cancelToken);
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

static void ThrowIfFailed(HRESULT hr)
{
	if (FAILED(hr))
	{
    OutputDebugString(L"bad hr");
		//throw Platform::Exception::CreateException(hr);
	}
}

task<String^> ImageUtilities::MakeTileSizedImage(IRandomAccessStream^ imageSource, String^ targetFileName, float height, float width, Windows::Storage::StorageFolder^ targetFolder, cancellation_token token)
{
	struct _stat64i32 stat;
	if (_wstat((targetFolder->Path + L"\\" + targetFileName)->Data(), &stat) == 0)
	{

		return task_from_result<String^>(targetFileName);
	}
	else
	{
		return create_task(targetFolder->CreateFileAsync(targetFileName, CreationCollisionOption::FailIfExists), token)
							 .then([=](task<StorageFile^> targetFileTask)
		{
			auto cleanupTargetFile = [=](Exception^ ex)
			{
				try
				{
					return create_task(targetFileTask.get()->DeleteAsync()).then([=](task<void> tsk)
					{
						try
						{
							tsk.get();
						}
						catch (...) {}
						return task_from_exception<String^>(ex);
					});
				}
				catch (...) {}
				return task_from_exception<String^>(ex);
			};

			try
			{
				auto targetFile = targetFileTask.get();
				return continue_task(targetFile->OpenAsync(FileAccessMode::ReadWrite),
					[=](IRandomAccessStream^ targetStream)
				{
					ComPtr<IWICImagingFactory> imagingFactory;
					ComPtr<IStream> pStream;
					ComPtr<IWICStream> pWicStream;
					ComPtr<IWICBitmapFrameDecode> baseBitmapFrame;
					ComPtr<IWICBitmapDecoder> bitmapDecoder;

					ThrowIfFailed(CoCreateInstance(CLSID_WICImagingFactory, NULL, CLSCTX_INPROC_SERVER,
						IID_IWICImagingFactory, (LPVOID*)&imagingFactory));

					imageSource->Seek(0);
					ThrowIfFailed(CreateStreamOverRandomAccessStream(imageSource, __uuidof(IStream), &pStream));

					ThrowIfFailed(imagingFactory->CreateStream(&pWicStream));
					ThrowIfFailed(pWicStream->InitializeFromIStream(pStream.Get()));
					ThrowIfFailed(imagingFactory->CreateDecoderFromStream(pWicStream.Get(), nullptr, WICDecodeMetadataCacheOnLoad, &bitmapDecoder));
					ThrowIfFailed(bitmapDecoder->GetFrame(0, &baseBitmapFrame));

					Microsoft::WRL::ComPtr<IWICBitmapSource> stageSource;
					Microsoft::WRL::ComPtr<IWICBitmapClipper> clipper;
					Microsoft::WRL::ComPtr<IWICBitmapScaler> scaler;
					Microsoft::WRL::ComPtr<IWICJpegFrameEncode> converter;

					ThrowIfFailed(baseBitmapFrame.As(&stageSource));

					UINT imageWidth, imageHeight;
					ThrowIfFailed(baseBitmapFrame->GetSize(&imageWidth, &imageHeight));
					GUID pixelFormat = { 0 };
					ThrowIfFailed(baseBitmapFrame->GetPixelFormat(&pixelFormat));

					auto frameScale = min(width / imageWidth, height / imageHeight);
					if (frameScale < 1.0)
					{
						auto targetRatio = width / height;
						auto currentRatio = static_cast<float>(imageWidth) / static_cast<float>(imageHeight);
						auto ratioDifference = std::abs(targetRatio - currentRatio);
						WICRect clipRect;
						if (targetRatio < currentRatio)
						{
							clipRect = { 0, 0, static_cast<int>(imageWidth * (targetRatio / currentRatio)), static_cast<int>(imageHeight) };
						}
						else
						{
							clipRect = { 0, 0, static_cast<int>(imageWidth), static_cast<int>(imageHeight * (currentRatio / targetRatio)) };
						}
						ThrowIfFailed(imagingFactory->CreateBitmapClipper(&clipper));
						ThrowIfFailed(clipper->Initialize(stageSource.Get(), &clipRect));
						ThrowIfFailed(clipper.As(&stageSource));
					}


					ThrowIfFailed(imagingFactory->CreateBitmapScaler(&scaler));
					ThrowIfFailed(scaler->Initialize(stageSource.Get(), width, height, WICBitmapInterpolationMode::WICBitmapInterpolationModeFant));
					ThrowIfFailed(scaler.As(&stageSource));


					ComPtr<IWICBitmapEncoder> encoder;
					//try hardware encoder first
					//if (CoCreateInstance(CLSID_WICJpegQualcommPhoneEncoder, NULL, CLSCTX_INPROC_SERVER,
					//	IID_IWICBitmapEncoder, (LPVOID*)&encoder) != S_OK)
					//{
						ThrowIfFailed(imagingFactory->CreateEncoder(GUID_ContainerFormatJpeg, 0, &encoder));
					//}


					ComPtr<IStream> targetComStream;
					ThrowIfFailed(CreateStreamOverRandomAccessStream(targetStream, __uuidof(IStream), &targetComStream));
					ThrowIfFailed(encoder->Initialize(
						targetComStream.Get(),
						WICBitmapEncoderNoCache));

					ComPtr<IWICBitmapFrameEncode> targetFrame;
					ThrowIfFailed(encoder->CreateNewFrame(&targetFrame, 0));
					ThrowIfFailed(targetFrame->Initialize(0));

					ThrowIfFailed(targetFrame->SetSize(width, height));
					ThrowIfFailed(targetFrame->SetPixelFormat(&pixelFormat));

					ThrowIfFailed(targetFrame->WriteSource(stageSource.Get(), 0));
					ThrowIfFailed(targetFrame->Commit());
					ThrowIfFailed(encoder->Commit());
					ThrowIfFailed(targetComStream->Commit(STGC_DEFAULT));
					return task_from_result(targetFolder->Path + L"\\" + targetFileName);
				}, cleanupTargetFile, token);
			}
			catch (concurrency::task_canceled)
			{
				return task_from_exception<String^>(ref new OperationCanceledException());
			}
			catch (Platform::Exception^ ex)
			{
				return task_from_result<String^>(targetFolder->Path + L"\\" + targetFileName);
			}
		});
	}
}

task<vector<ImageInfo^>> ImageUtilities::MakeLiveTileImages(vector<ImageInfo^> liveTileFiles, LockScreenHistory^ history, vector<tuple<String^, String^>> liveTileTpls, int targetCount, int targetIndex, concurrency::cancellation_token cancelToken)
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
		struct _stat64i32 stat;
		if (_wstat(wideFilePath.c_str(), &stat) == 0 && _wstat(squareFilePath.c_str(), &stat) == 0)
		{
			return MakeLiveTileImages(liveTileFiles, history, liveTileTpls, targetCount, targetIndex + 1, cancelToken);
		}
		else
		{
			return GetImageSource(targetUrl, cancelToken)
				.then([=](task<IRandomAccessStream^> imageSourceTask)
			{
				try
				{
					auto imageSource = imageSourceTask.get();
					if (imageSource != nullptr)
					{
						return MakeTileSizedImage(imageSource, wideFileName, 150, 310, Windows::Storage::ApplicationData::Current->LocalFolder, cancelToken)
							.then([=](task<String^> filePath)
						{
							return MakeTileSizedImage(imageSource, squareFileName, 150, 150, Windows::Storage::ApplicationData::Current->LocalFolder, cancelToken)
								.then([=](task<String^> smallFilePath)
							{
								try
								{
									if (filePath.get() == nullptr || smallFilePath.get() == nullptr)
										madeImageInfo->Faulted = true;
								}
								catch (...) {};


								return MakeLiveTileImages(liveTileFiles, history, liveTileTpls, targetCount, targetIndex + 1, cancelToken);
							});
						});
					}
					else
					{
						madeImageInfo->Faulted = true;
						return MakeLiveTileImages(liveTileFiles, history, liveTileTpls, targetCount, targetIndex + 1, cancelToken);
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
			return continue_task(GetImageSource(url, token), [=](IRandomAccessStream^ source)
			{
				return MakeTileSizedImage(source, resultFileName, height, width, Windows::Storage::ApplicationData::Current->TemporaryFolder, token);
			}, [](Platform::Exception^ ex) { return task_from_exception<String^>(ex); });	
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