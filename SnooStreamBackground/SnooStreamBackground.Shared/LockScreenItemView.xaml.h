//
// LockScreenItemView.xaml.h
// Declaration of the LockScreenItemView class
//

#pragma once

#include "LockScreenItemView.g.h"
#include "LockScreenViewModel.h"

namespace SnooStreamBackground
{
	[Windows::Foundation::Metadata::WebHostHidden]
	public ref class LockScreenItemView sealed
	{
	public:
		LockScreenItemView(LockScreenMessage^ lockScreenMessage);
	};
}
