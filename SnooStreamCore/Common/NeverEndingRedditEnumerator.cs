using GalaSoft.MvvmLight;
using SnooSharp;
using SnooStream.Services;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public abstract class NeverEndingRedditEnumerator
    {
        public static NeverEndingRedditEnumerator MakeEnumerator(ViewModelBase context, object currentPosition, bool forward)
        {
            if (context is LinkRiverViewModel)
                return new NeverEndingRedditLinkEnumerator(context as LinkRiverViewModel, (int)currentPosition, forward);
            else if (context is CommentsViewModel)
                return new NeverEndingRedditCommentsEnumerator(context as CommentsViewModel, currentPosition as string, forward);
            else
                throw new ArgumentException("invalid context type");
                
        }
        public abstract Task<ViewModelBase> Next();
    }

    class NeverEndingRedditLinkEnumerator : NeverEndingRedditEnumerator
    {
        LinkRiverViewModel _context;
        int _currentLinkPos;
        bool _forward;
        public NeverEndingRedditLinkEnumerator(LinkRiverViewModel context, int currentLinkPos, bool forward)
        {
            _context = context;
            _currentLinkPos = currentLinkPos + (forward ? 0 : 1);
            _forward = forward;
        }
        public override async Task<ViewModelBase> Next()
        {
            if (_forward)
            {
                _currentLinkPos++;
                if (_context.Links.Count <= _currentLinkPos || (_currentLinkPos == 0 && _context.Links.Count == 0))
                {
                    await _context.LoadMore();
                }
            }
            else
                _currentLinkPos--;

            if (_context.Links.Count > _currentLinkPos && _currentLinkPos > -1)
                return _context.Links[_currentLinkPos];
            else
                return null;
        }
    }

    class NeverEndingRedditCommentsEnumerator : NeverEndingRedditEnumerator
    {
        CommentsViewModel _context;
        int _currentLinkPos;
        bool _forward;
        List<Tuple<string, string, CommentViewModel>> _links;
        public NeverEndingRedditCommentsEnumerator(CommentsViewModel context, string currentLinkPos, bool forward)
        {
            _context = context;
            _forward = forward;
            foreach (var comment in _context.FlatComments)
            {
                var commentVm = comment as CommentViewModel;
                if(commentVm != null)
                {
                    foreach(var commentLinkTpl in SnooStreamViewModel.MarkdownProcessor.GetLinks(commentVm.BodyMD as MarkdownData))
                    {
                        //need to be -1 even though we havent accounted for the add of the current target, because movenext does a ++ before it gets the current
                        if (currentLinkPos != null && commentLinkTpl.Item1 == currentLinkPos)
                            _currentLinkPos = _links.Count - 1;

                        _links.Add(Tuple.Create(commentLinkTpl.Item1, commentLinkTpl.Item2, commentVm));
                    }
                }
            }
        }
        public override Task<ViewModelBase> Next()
        {
            if (_forward)
            {
                _currentLinkPos++;
            }
            else
                _currentLinkPos--;

            if (_links.Count > _currentLinkPos && _currentLinkPos > 0)
                return Task.FromResult<ViewModelBase>(new LinkViewModel(_links[_currentLinkPos].Item3, new Link { Url = _links[_currentLinkPos].Item1, Title = _links[_currentLinkPos].Item2 }));
            else
                return Task.FromResult<ViewModelBase>(null);
        }
    }
}
