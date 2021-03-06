﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Services
{
    public interface IMarkdownProcessor
    {
        MarkdownData Process(string markdown);
		IEnumerable<KeyValuePair<string, string>> GetLinks(MarkdownData mkd);
		bool IsPlainText(MarkdownData mdk);
		string BasicText(MarkdownData mdk);
    }
    public class MarkdownData
    {
        public object MarkdownDom { get; set; }
    }
}
