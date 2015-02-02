#include "pch.h"
#include "NetworkUtilities.h"

using namespace SnooStreamBackground;
using namespace Windows::Networking::Connectivity;


bool NetworkUtilities::LowPriorityNetworkOk()
{
    auto connectionProfile = NetworkInformation::GetInternetConnectionProfile();
    if (connectionProfile == nullptr || connectionProfile->GetNetworkConnectivityLevel() != NetworkConnectivityLevel::InternetAccess)
        return false;

    auto connectionCost = connectionProfile->GetConnectionCost();
	if (connectionCost == nullptr)
		return false;

    auto connectionCostType = connectionCost->NetworkCostType;
	auto signalBars = connectionProfile->GetSignalBars();
    auto connectionStrength = signalBars != nullptr ? 
		signalBars->Value : 
		connectionProfile->IsWwanConnectionProfile ? 0 : 5;
    if (connectionCostType != NetworkCostType::Unrestricted && connectionCostType != NetworkCostType::Unknown)
        return false;

    if (connectionProfile->IsWwanConnectionProfile)
    {
        auto connectionClass = connectionProfile->WwanConnectionProfileDetails->GetCurrentDataClass();
        switch (connectionClass)
        {
            case WwanDataClass::Hsdpa:
            case WwanDataClass::Hsupa:
            case WwanDataClass::LteAdvanced:
            case WwanDataClass::Umts:
                break;
            default:
                return false;
        }

        if (connectionStrength < 3)
            return false;
    }

    return !(connectionCost->ApproachingDataLimit || connectionCost->OverDataLimit || connectionCost->Roaming);
}

bool NetworkUtilities::IsHighPriorityNetworkOk()
{
    auto connectionProfile = NetworkInformation::GetInternetConnectionProfile();
    if (connectionProfile == nullptr || connectionProfile->GetNetworkConnectivityLevel() != NetworkConnectivityLevel::InternetAccess)
        return false;

    auto connectionCost = connectionProfile->GetConnectionCost();
	
    return connectionCost != nullptr && (connectionProfile->GetSignalBars() != nullptr || !connectionProfile->IsWwanConnectionProfile) && !connectionCost->OverDataLimit;
}