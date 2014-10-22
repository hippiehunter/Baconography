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

		int _loadPhase = 0;
		private MarkdownData _markdownBody;
		private CancellationTokenSource _loadCancel = new CancellationTokenSource();
		internal async void PhaseLoad(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (!args.InRecycleQueue)
			{
				switch (_loadPhase)
				{
					case 0:
						{
							plainTextControl.Opacity = 0.0;
							markdownControl.Opacity = 0.0;

							if (args.Item is CommentViewModel)
							{
								_loadPhase++;
								var body = ((CommentViewModel)args.Item).Body;
								args.Handled = true;
								args.RegisterUpdateCallback(PhaseLoad);
								var markdownBody = await Task.Run(() => SnooStreamViewModel.MarkdownProcessor.Process(body), _loadCancel.Token);
								if (!_loadCancel.IsCancellationRequested)
									_markdownBody = markdownBody;

								
							}
							
							break;
						}
					case 1:
						{
							_loadPhase++;
							plainTextControl.Opacity = 1.0;
							if(args.Item is CommentViewModel)
								plainTextControl.Text = ((CommentViewModel)args.Item).Body;
							
							args.RegisterUpdateCallback(PhaseLoad);
							break;
						}
					case 2:
						{
							if (_markdownBody != null)
							{
								if (!SnooStreamViewModel.MarkdownProcessor.IsPlainText(_markdownBody))
								{
									plainTextControl.Opacity = 0.0;
									plainTextControl.Text = "";

									markdownControl.Opacity = 1.0;
									markdownControl.Markdown = _markdownBody.MarkdownDom as SnooDom.SnooDom;
								}
							}
							else
							{
								args.RegisterUpdateCallback(PhaseLoad);
							}
							break;
						}
				}
			}
			else
			{
				try
				{
					_loadPhase = 0;
					_loadCancel.Cancel();
					_loadCancel = new CancellationTokenSource();
					if (_markdownBody != null)
					{
						_markdownBody.MarkdownDom = null;
						_markdownBody = null;
					}
				}
				catch
				{
					
				}
			}
		}
	}
}
