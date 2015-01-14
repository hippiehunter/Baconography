using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel.Content
{
	public class SelfViewModel : ContentViewModel
	{
		private LinkViewModel _selfLink;

		public SelfViewModel(LinkViewModel selfLink)
		{
			_selfLink = selfLink;
            UIThreadLoad = true;
		}


        public CommentsViewModel Comments
        {
            get
            {
                return _selfLink.Comments;
            }
        }

		public string SelfText
		{
			get
			{
				return SnooStreamViewModel.MarkdownProcessor.BasicText(_selfLink.SelfText);
			}
		}

        public object Markdown
        {
            get
            {
                return _selfLink.SelfText.MarkdownDom;
            }
        }

		protected override Task StartLoad()
		{
			return _selfLink.Comments.LoadFull();
		}
	}
}
