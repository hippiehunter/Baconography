using SnooDom;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;

namespace SnooStream.Common
{
    class MarkdownHelpers : IStyleProvider, ICommandFactory
    {
		public Windows.Foundation.TypedEventHandler<Windows.UI.Xaml.Documents.Hyperlink, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs> MakeLinkCommand(string url)
		{
			return new Windows.Foundation.TypedEventHandler<Windows.UI.Xaml.Documents.Hyperlink, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs>((link, bla) => { });
		}

		public Windows.UI.Xaml.Style BorderStyle
		{
			get { return null; }
		}

		public Windows.UI.Xaml.Style RichTextBlockStyle
		{
			get { return null; }
		}

		public Windows.UI.Xaml.Style RunStyle
		{
			get { return null; }
		}

		public Windows.UI.Xaml.Style TextBlockStyle
		{
            get { return App.Current.Resources["MarkdownTextBlockStyle"] as Style; }
		}
	}
}
