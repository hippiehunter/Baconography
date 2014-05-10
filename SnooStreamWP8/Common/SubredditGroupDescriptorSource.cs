using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Windows.Data;

namespace SnooStreamWP8.Common
{
	public class SubredditGroupDescriptorSource
	{
		public SubredditGroupDescriptorSource ()
		{
			Descriptors = new List<DataDescriptor>
			{
				new GenericGroupDescriptor<LinkRiverViewModel, string>(ClassifyLinkRiver)
			};
		}

		public static string ClassifyLinkRiver (LinkRiverViewModel linkRiver)
		{
			return SubredditRiverViewModel.SmallGroupNameSelector(linkRiver);
		}


		public List<DataDescriptor> Descriptors { get; private set; }
	}
}
