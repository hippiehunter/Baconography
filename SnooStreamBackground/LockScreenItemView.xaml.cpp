﻿//
// LockScreenItemView.xaml.cpp
// Implementation of the LockScreenItemView class
//

#include "pch.h"
#include "LockScreenItemView.xaml.h"

using namespace SnooStreamBackground;

using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::UI::Xaml;
using namespace Windows::UI::Xaml::Controls;
using namespace Windows::UI::Xaml::Controls::Primitives;
using namespace Windows::UI::Xaml::Data;
using namespace Windows::UI::Xaml::Input;
using namespace Windows::UI::Xaml::Media;
using namespace Windows::UI::Xaml::Navigation;


LockScreenItemView::LockScreenItemView(LockScreenMessage^ lockScreenMessage)
{
	InitializeComponent();
	glyph->Text = lockScreenMessage->Glyph;
	displayText->Text = lockScreenMessage->DisplayText;
}