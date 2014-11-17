using SnooStream.Services;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SnooStream.View.Controls
{
    public partial class CommentView : UserControl
    {
        public CommentView()
        {
            InitializeComponent();
        }

		internal void PhaseLoad(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (!args.InRecycleQueue)
			{
				switch (args.Phase)
				{
					case 0:
						{
							plainTextControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
							markdownControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
							markdownControl.Markdown = null;
							args.Handled = true;
							args.RegisterUpdateCallback(PhaseLoad);
							break;
						}
					case 1:
						{
							plainTextControl.Visibility = Windows.UI.Xaml.Visibility.Visible;
							if (args.Item is CommentViewModel)
								plainTextControl.Text = ((CommentViewModel)args.Item).Body;

							args.Handled = true;
							args.RegisterUpdateCallback(PhaseLoad);
							break;
						}
					case 2:
						{
							if (args.Item is CommentViewModel)
							{
								var body = ((CommentViewModel)args.Item).Body;
								args.Handled = true;
								var markdownBody = SnooStreamViewModel.MarkdownProcessor.Process(body);

								if (!SnooStreamViewModel.MarkdownProcessor.IsPlainText(markdownBody))
								{
									plainTextControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
									plainTextControl.Text = "";

									markdownControl.Visibility = Windows.UI.Xaml.Visibility.Visible;
									markdownControl.Markdown = markdownBody.MarkdownDom as SnooDom.SnooDom;
								}
							}
							break;
						}
				}
			}
			else
			{
			}
		}
	}
}
