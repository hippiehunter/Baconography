//
// LockScreenViewControl.xaml.h
// Declaration of the LockScreenViewControl class
//

#pragma once

#include "LockScreenViewControl.g.h"
#include "LockScreenViewModel.h"

namespace SnooDomBackground
{
	[Windows::Foundation::Metadata::WebHostHidden]
	public ref class LockScreenViewControl sealed
	{
	internal:
		LockScreenViewControl(LockScreenViewModel^ viewModel);
	};
}
