using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Services
{
    public interface IMarkdownProcessor
    {
        MarkdownData Process(string markdown);
        IEnumerable<Tuple<string, string>> GetLinks(MarkdownData mkd);
    }
    public class MarkdownData
    {
        public object MarkdownDom { get; set; }
    }
}
