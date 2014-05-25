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
		public MoreViewModel(CommentsViewModel context, string parentId, string id, List<string> ids, int count, int depth = 0)
        {
			_context = context;
			Depth = depth;
			Id = id;
			ParentId = parentId;
			Loading = false;
			Ids = ids;
			Count = count == 0 ? ids.Count : count;
			_triggerLoad = new RelayCommand(async () => await _context.LoadMore(this));
        }

		public List<string> Ids { get; set; }
        public string Id { get; set; }
		public bool Loading { get; set; }
		public int Depth { get; set; }
		public string CountString
		{
			get
			{
				return Count.ToString();
			}
		}
		public int Count { get; set; }
		public string ParentId { get; set; }
		public CommentViewModel Parent
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(ParentId))
				{
					return _context.GetById(ParentId);
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

		internal void TouchImpl()
		{
			RaisePropertyChanged("IsVisible");
		}
    }
}
