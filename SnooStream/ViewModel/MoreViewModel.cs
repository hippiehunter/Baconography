using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SnooStream.ViewModel
{
    public class MoreViewModel : ViewModelBase
    {
		CommentsViewModel _context;
		string _parentId;
		List<string> _ids;
		public MoreViewModel(CommentsViewModel context, string parentId, string id, List<string> ids, int depth = 0)
        {
			_context = context;
			Depth = depth;
			Id = id;
			_parentId = parentId;
			Loading = false;
			_ids = ids;
			CountString = ids.Count.ToString();
			_triggerLoad = new RelayCommand(async () => await _context.LoadMore(new SnooSharp.More{ Children = _ids }));
        }

        public string Id { get; set; }
		public bool Loading { get; set; }
		public int Depth { get; set; }
		public string CountString { get; set; }
		public CommentViewModel Parent
		{
			get
			{
				var parentId = (_parentId ?? "").StartsWith("t1_") ? _parentId : null;
				if (!string.IsNullOrWhiteSpace(parentId))
				{
					return _context.GetById(parentId);
				}
				else
					return null;
			}
		}

		public bool IsVisible
		{
			get
			{
				var parent = Parent;
				if (parent != null)
				{
					return parent.IsVisible ? !parent.IsMinimized : false;
				}
				return true;
			}
		}

		RelayCommand _triggerLoad;
		public RelayCommand TriggerLoad
		{
			get
			{
				return _triggerLoad;
			}
		}
    }
}
