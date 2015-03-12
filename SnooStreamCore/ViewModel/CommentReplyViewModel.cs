using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SnooSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class CommentReplyViewModel : ViewModelBase
    {
        CommentViewModel _context;
        Thing _replyTarget;
        public CommentReplyViewModel(CommentViewModel context, Thing replyTarget, bool isEdit = false)
        {
            _replyTarget = replyTarget;
            _context = context;
            if (isEdit)
            {
                Editing = true;
                EditingId = ((Comment)replyTarget.Data).Name;
				_text = ((Comment)replyTarget.Data).Body;
            }
			else
			{
				_text = "";
			}
        }

		MarkdownEditingVM EditingVM
		{
			get
			{
				return new MarkdownEditingVM(_text, (value) => _text = value);
			}
		}

		private string _text;
        public bool Editing { get; set; }
        public string EditingId { get; set; }

        private async void SubmitImpl()
        {
            bool edit = Editing && !string.IsNullOrEmpty(EditingId);

            await SnooStreamViewModel.NotificationService.Report(edit ? "updating comment" : "adding reply", async () =>
                {
                    if (edit)
                    {
						await SnooStreamViewModel.RedditService.EditComment(EditingId, _text);
                    }
                    else
                    {
                        var parentId = ((Comment)_replyTarget.Data).ParentId;
                        if (!parentId.StartsWith("t1_") && !parentId.StartsWith("t3_"))
                            parentId = "t1_" + parentId;
						var addedComment = await SnooStreamViewModel.RedditService.AddComment(parentId, _text);
                        if (addedComment != null)
                        {
                            _context.Rename(addedComment);
                        }
                    }
                    var theComment = new Thing
                    {
                        Kind = "t1",
                        Data = new Comment
                        {
							Author = string.IsNullOrWhiteSpace(SnooStreamViewModel.RedditUserState.Username) ? "self" : SnooStreamViewModel.RedditUserState.Username,
							Body = _text,
                            Likes = true,
                            Ups = 1,
                            ParentId = ((dynamic)_replyTarget.Data).Name,
                            Name = EditingId,
                            Replies = new Listing { Data = new ListingData { Children = new List<Thing>() } },
                            Created = DateTime.Now,
                            CreatedUTC = DateTime.UtcNow
                        }
                    };

                    _context.IsEditing = false;
                });
        }

        public RelayCommand Cancel
        {
            get
            {
                return new RelayCommand(() =>
                 {
                     if (Editing)
                         _context.IsEditing = false;
                     else
                         _context.RemoveFromContext();
                 });
            }
        }
    }
}
