using SnooStream.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using Windows.UI.Xaml.Controls;

namespace SnooStream.View.Pages
{
	public partial class SnooHubMark2 : SnooApplicationPage
	{
		public SnooHubMark2()
		{
			InitializeComponent();
		}

        public override bool DefaultSystray
        {
            get
            {
                return false;
            }
        }
	}
}