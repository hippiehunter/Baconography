using GalaSoft.MvvmLight;
using SnooSharp;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public class ObservableOrderedLinkGroup : ViewModelBase, INotifyCollectionChanged, ICollection<LinkViewModel>, IList<LinkViewModel>
    {
        private Dictionary<string, LinkViewModel> _nameMap = new Dictionary<string, LinkViewModel>();
        private List<LinkViewModel> _elements = new List<LinkViewModel>();
        ViewModelBase _context;
        public ObservableOrderedLinkGroup(ViewModelBase context, IEnumerable<Link> initialValues)
        {
            _context = context;
            foreach (var element in initialValues)
            {
                if (_nameMap.ContainsKey(element.Name))
                    continue;
                else
                {
                    var newViewModel = new LinkViewModel(context, element);
                    _nameMap.Add(newViewModel.Link.Name, newViewModel);
                    _elements.Add(newViewModel);
                }
            }
        }

        public void UpdateElements(IList<Link> replacements)
        {
            Dictionary<string, Tuple<int, int, LinkViewModel>> namePosMap = new Dictionary<string, Tuple<int, int, LinkViewModel>>();
            for (int i = 0; i < _elements.Count; i++)
            {
                if (namePosMap.ContainsKey(_elements[i].Link.Name))
                    continue;
                else
                {
                    namePosMap.Add(_elements[i].Link.Name, Tuple.Create(i, -1, _elements[i]));
                }
            }

            for (int i = 0; i < replacements.Count; i++)
            {
                if (namePosMap.ContainsKey(replacements[i].Name))
                {
                    var existing = namePosMap[replacements[i].Name];
                    existing.Item3.MergeLink(replacements[i]);
                    namePosMap[replacements[i].Name] = Tuple.Create(existing.Item1, i, existing.Item3);
                }
                else
                {
                    var newViewModel = new LinkViewModel(_context, replacements[i]);
                    namePosMap.Add(replacements[i].Name, Tuple.Create(-1, i, newViewModel));
                }
            }

            foreach (var tpl in namePosMap.Values)
            {
                if (tpl.Item1 == tpl.Item2)
                    continue;
                else if(tpl.Item2 != -1)
                {
                    //update element
                    _elements[tpl.Item2] = tpl.Item3;
                    if (CollectionChanged != null)
                    {
                        //this doesnt work right, cant figure out how to correctly deal with move/replace/delete/add while maintaining collection consistancy
                        CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, tpl.Item3, tpl.Item2));
                    }
                    
                }
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void Add(LinkViewModel item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(LinkViewModel item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(LinkViewModel[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public bool Remove(LinkViewModel item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<LinkViewModel> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public int IndexOf(LinkViewModel item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, LinkViewModel item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public LinkViewModel this[int index]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}
