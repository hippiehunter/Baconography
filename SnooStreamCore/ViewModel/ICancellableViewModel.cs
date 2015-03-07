using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public interface ICancellableViewModel
    {
        /// <summary>
        /// called by SnooApplicationPage when a view model is bound, that wants a cancelationToken for the current context
        /// </summary>
        /// <param name="token"></param>
        /// <returns>true if Cancel should happen on all context changes, false if only on back transition</returns>
        bool BindToken(CancellationToken token);
    }
}
