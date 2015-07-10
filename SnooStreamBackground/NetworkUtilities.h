#pragma once

namespace SnooStreamBackground
{
    public ref class NetworkUtilities sealed
    {
    public:
        static bool LowPriorityNetworkOk();
        static bool IsHighPriorityNetworkOk();
    };
}