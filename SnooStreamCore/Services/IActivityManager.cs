using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnooSharp;

namespace SnooStream.Services
{
    public interface IActivityManager
    {
        bool NeedsRefresh();
        Task Refresh();
        string OAuth { set; }
        Listing Sent { get; }
        Listing Received { get; }
        Listing Activity { get; }
        Listing ContextForId(string id);
    }
}
