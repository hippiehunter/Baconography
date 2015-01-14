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
                            contentControl.ContentTemplate = null;
                            contentControl.Content = null;
							//markdownControl.Markdown = null;
							args.Handled = true;
							args.RegisterUpdateCallback(PhaseLoad);
							break;
						}
					case 1:
						{
                            //plainTextControl.Visibility = Windows.UI.Xaml.Visibility.Visible;
                            if (args.Item is CommentViewModel)
                            {
                                contentControl.ContentTemplate = Resources["textTemplate"] as DataTemplate;
                                contentControl.Content = ((CommentViewModel)args.Item).Body;
                            }

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
                                    contentControl.ContentTemplate = Resources["markdownTemplate"] as DataTemplate;
                                    contentControl.Content = markdownBody.MarkdownDom;
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
