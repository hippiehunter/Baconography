using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Messages
{
	public class FocusChangedMessage : MessageBase
	{
		public FocusChangedMessage(object sender) : base(sender) { }
	}
}
