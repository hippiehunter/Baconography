#include "pch.h"
#include "ActivityManager.h"

#include <fstream>
#include <string>

using namespace std;
using namespace SnooStreamBackground;

ActivityManager::ActivityManager()
{
    //load already toasted list
    wstring historyPath(Windows::Storage::ApplicationData::Current->LocalFolder->Path->Data());
    historyPath += L"\\bgtaskMessageHistory.txt";
    wifstream existingMessagesFile(historyPath, std::ios_base::in | std::ios_base::beg, _SH_DENYRW);
    wstring existingMessageLine;
    while (getline(existingMessagesFile, existingMessageLine))
    {
        if (_alreadyToasted.find(existingMessageLine) != _alreadyToasted.end())
            _alreadyToasted.insert(existingMessageLine);
    }
    existingMessagesFile.close();
    //grab blob activity blob from disk
}
