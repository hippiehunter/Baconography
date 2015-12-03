using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public interface ISearchContext
    {
    }

    public class SearchViewModel
    {
        private ISearchContext searchContext;

        public SearchViewModel(ISearchContext searchContext)
        {
            this.searchContext = searchContext;
        }
    }

    public class SearchContext : ISearchContext
    {

    }
}
