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


        public IEnumerable<Tuple<string, string>> GetLinks(MarkdownData mkd)
        {
            var document = mkd.MarkdownDom as Document;
            if (document != null)
            {
                var linkVisitor = new SnooDomLinkVisitor();
                linkVisitor.Visit(document);
                List<Tuple<string, string>> result = new List<Tuple<string, string>>();
                foreach (var link in linkVisitor.Links)
                {
                    result.Add(Tuple.Create(link.Url, link.Hover != null ? link.Hover.Contents : link.Url));
                }
                return result;
            }
            else
                return Enumerable.Empty<Tuple<string, string>>();
        }
    }
}
