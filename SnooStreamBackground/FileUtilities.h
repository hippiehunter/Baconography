#pragma once

#include <string>
#include <fstream>

Platform::String^ readFileWithLock(std::wstring fileName);
void writeFileWithLock(Platform::String^ data, std::wstring fileName, bool truncate);