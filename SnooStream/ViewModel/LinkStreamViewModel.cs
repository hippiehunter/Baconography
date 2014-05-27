using GalaSoft.MvvmLight;
using SnooStream.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    //provides current link position for flip view
    //provides load more behavior
    public class LinkStreamViewModel : ViewModelBase
    {
        private ViewModelBase _context;
        private NeverEndingRedditEnumerator _linkRiverEnumerator;
        public LinkStreamViewModel(ViewModelBase context, string targetIdent)
        {
            _context = context;
            object targetIndex = targetIdent;
            var linkRiver = context as LinkRiverViewModel;
            if (linkRiver != null)
            {
                int linkIndex = -1;
                if (targetIdent != null)
                {
                    for (int i = 0; i < linkRiver.Links.Count; i++)
                    {
                        if (linkRiver.Links[i].Link.Id == targetIdent)
                        {
                            //need to be -1 because movenext does ++ before it gets current
                            linkIndex = i - 1;
                            break;
                        }
                    }
                }
                targetIndex = linkIndex;
                
                
            }
            else if(context is CommentsViewModel)
                throw new ArgumentException("invalid link stream context");

            _linkRiverEnumerator = NeverEndingRedditEnumerator.MakeEnumerator(_context, targetIndex, true);
            LoadPrior = new Lazy<NeverEndingRedditEnumerator>(() => targetIndex != null ? NeverEndingRedditEnumerator.MakeEnumerator(_context, targetIndex, false) : null);
        }

        public Lazy<NeverEndingRedditEnumerator> LoadPrior { get; private set; }

		public ContentViewModel Visible { get; set; }
        public LinkViewModel Current { get; private set; }

        public async Task<bool> MoveNext()
        {
            try
            {
                var result = await _linkRiverEnumerator.Next();
                if (result is LinkViewModel)
                {
                    Current = result as LinkViewModel;
                    return true;
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
