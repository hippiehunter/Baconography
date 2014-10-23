using SnooDom;
using SnooStream.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.PlatformServices
{
    class MarkdownProvider : IMarkdownProcessor
    {
        public MarkdownData Process(string markdown)
        {
            var processed = SnooDom.SnooDom.MarkdownToDOM(System.Net.WebUtility.HtmlDecode(markdown));
			return new MarkdownData { MarkdownDom = processed };
        }

		public IEnumerable<KeyValuePair<string, string>> GetLinks(MarkdownData mkd)
        {
			return ((SnooDom.SnooDom)mkd.MarkdownDom).GetLinks();
        }


		public bool IsPlainText(MarkdownData mkd)
		{
			return ((SnooDom.SnooDom)mkd.MarkdownDom).IsPlainText();
		}


		public string BasicText(MarkdownData mkd)
		{
			return ((SnooDom.SnooDom)mkd.MarkdownDom).BasicText();
		}
	}
}
