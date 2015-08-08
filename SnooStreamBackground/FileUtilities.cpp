#include "pch.h"
#include "FileUtilities.h"
#include <vector>

Platform::String^ readFileWithLock(std::wstring fileName)
{
  auto targetFile = CreateFile2(fileName.c_str(), GENERIC_READ | GENERIC_WRITE, 0, OPEN_EXISTING, nullptr);
  LARGE_INTEGER fileSize;
  if (targetFile != INVALID_HANDLE_VALUE && GetFileSizeEx(targetFile, &fileSize))
  {
    OVERLAPPED overlapped = {};
    if (LockFileEx(targetFile, LOCKFILE_EXCLUSIVE_LOCK, 0, fileSize.LowPart, fileSize.HighPart, &overlapped))
    {
      std::vector<wchar_t> bytes(fileSize.QuadPart / 2);
      DWORD writtenSize;
      if (ReadFile(targetFile, &bytes[0], fileSize.QuadPart, &writtenSize, nullptr))
      {
        UnlockFileEx(targetFile, 0, fileSize.LowPart, fileSize.HighPart, &overlapped);
        CloseHandle(targetFile);
        auto resultString = ref new Platform::String(&bytes[0], fileSize.QuadPart / 2);
        return resultString;
      }
    }
  }

  return L"";
}

void writeFileWithLock(Platform::String^ data, std::wstring fileName, bool truncate)
{
  auto targetFile = CreateFile2(fileName.c_str(), GENERIC_READ | GENERIC_WRITE, 0, truncate ? CREATE_ALWAYS : CREATE_NEW, nullptr);
  if (targetFile != INVALID_HANDLE_VALUE)
  {
    LARGE_INTEGER fileSize;
    fileSize.QuadPart = data->Length() * 2;
    OVERLAPPED overlapped = {};
    if (LockFileEx(targetFile, LOCKFILE_EXCLUSIVE_LOCK, 0, fileSize.LowPart, fileSize.HighPart, &overlapped))
    {
      //only reposition to the end if we arent doing a truncation, otherwise its already there
      if (truncate || SetFilePointerEx(targetFile, LARGE_INTEGER{ 0 }, nullptr, FILE_END))
      {
        DWORD bytesWritten;
        if (WriteFile(targetFile, data->Data(), fileSize.QuadPart, &bytesWritten, nullptr))
        {
          if (FlushFileBuffers(targetFile) &&
            UnlockFileEx(targetFile, 0, fileSize.LowPart, fileSize.HighPart, &overlapped) &&
            CloseHandle(targetFile))
          {
            return;
          }
        }
      }
    }
  }
  throw ref new Platform::AccessDeniedException();
}