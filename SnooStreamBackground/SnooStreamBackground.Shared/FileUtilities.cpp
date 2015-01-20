#include "pch.h"
#include "FileUtilities.h"
#include <vector>

Platform::String^ readFileWithLock(std::wstring fileName)
{
	std::wifstream ifs(fileName, std::ios_base::in | std::ios_base::binary | std::ios_base::ate, _SH_DENYRW);
	auto fileSize = ifs.tellg();
	if (ifs.is_open() && fileSize > 0)
	{
		ifs.seekg(0, std::ios::beg);
		std::vector<wchar_t> bytes(fileSize);
		ifs.read(&bytes[0], fileSize);
		auto resultString = ref new Platform::String(&bytes[0], fileSize);
		return resultString;
	}
	else
		return L"";
}