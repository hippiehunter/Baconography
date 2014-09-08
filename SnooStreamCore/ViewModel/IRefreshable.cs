using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public interface IRefreshable
    {
        void MaybeRefresh();
        void Refresh();
    }
}
