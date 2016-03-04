using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public interface ISelfContext
    {
    }

    public class SelfViewModel
    {
        private ISelfContext selfContext;

        public SelfViewModel(ISelfContext selfContext)
        {
            this.selfContext = selfContext;
        }

        public string Username { get; set; }
    }

    class SelfContext : ISelfContext
    {

    }
}
