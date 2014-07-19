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
        public SnooDom.SnooDom Process(string markdown)
        {
            var processed = SnooDom.SnooDom.MarkdownToDOM(System.Net.WebUtility.HtmlDecode(markdown));
			return processed;
        }


		public IEnumerable<KeyValuePair<string, string>> GetLinks(SnooDom.SnooDom mkd)
        {
			return mkd.GetLinks();
        }
    }
}
