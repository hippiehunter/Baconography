#pragma once

#include "boost\algorithm\string.hpp"

namespace SnooDomBackground
{
	public ref class LockScreenMessage sealed
	{
		Platform::String^ _displayText;
	public:
		property Platform::String^ DisplayText
		{
			Platform::String^ get()
			{
				return _displayText;
			}
			void set(Platform::String^ value)
			{
				std::wstring strValue(value->Begin(), value->End());
				boost::replace_all(strValue, "\r", "");
				boost::replace_all(strValue, "\n", "");
				boost::replace_all(strValue, "&amp;", "&");
				boost::replace_all(strValue, "&lt;", "<");
				boost::replace_all(strValue, "&gt;", ">");
				boost::replace_all(strValue, "&quot;", "\"");
				boost::replace_all(strValue, "&apos;", "'");
				boost::trim(strValue);

				if (strValue.size() > 100)
					_displayText = ref new Platform::String(strValue.data(), 100);
				else
					_displayText = ref new Platform::String(strValue.data(), strValue.size());
			}
		}
		property Platform::String^ Glyph;
	};

	public ref class LockScreenViewModel sealed
	{
	public:
		property Platform::String^ ImageSource;
		property Windows::Foundation::Collections::IVector<LockScreenMessage^>^ OverlayItems;
		property int NumberOfItems;
		property bool RoundedCorners;
		property Windows::UI::Xaml::CornerRadius CornerRadius;
		property Windows::UI::Xaml::Thickness Margin;
		property Windows::UI::Xaml::Thickness InnerMargin;
		property float OverlayOpacity;
	};

	
}
