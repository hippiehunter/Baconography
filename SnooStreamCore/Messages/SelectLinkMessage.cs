using GalaSoft.MvvmLight.Messaging;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Messages
{
	public class SelectLinkMessage : MessageBase
	{
		public enum LinkSelectionKind
		{
			Comments,
			FollowLink,
			Content
		}

		public LinkViewModel Link { get; set; }
		public LinkSelectionKind Kind { get; set; }
	}
}
