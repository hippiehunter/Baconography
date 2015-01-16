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

        public int LoadPhase;
        public CancellationTokenSource LoadCancelSource = new CancellationTokenSource();

        internal bool Phase0Load(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (!args.InRecycleQueue)
            {
                contentControl.ContentTemplate = null;
                contentControl.Content = null;
                var body = ((CommentViewModel)args.Item).Body;
                contentControl.MinHeight = Math.Max(25, body.Length / 2);
                args.Handled = true;
                LoadPhase = 1;
                return true;
            }
            return false;
        }

        internal bool Phase1Load(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (!args.InRecycleQueue)
            {
                if (args.Item is CommentViewModel)
                {
                    contentControl.MinHeight = 0;
                    contentControl.ContentTemplate = Resources["textTemplate"] as DataTemplate;
                    contentControl.Content = ((CommentViewModel)args.Item).Body;
                    args.Handled = true;
                    LoadPhase = 2;
                    return true;
                }
                else
                    throw new NotImplementedException();
            }
            return false;
        }

        internal async void Phase2Load(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (!args.InRecycleQueue)
            {
                if (args.Item is CommentViewModel)
                {
                    var body = ((CommentViewModel)args.Item).Body;
                    args.Handled = true;
                    var loadToken = LoadCancelSource.Token;
                    var markdownTpl = await Task.Run(() => 
                        {
                            try
                            {
                                var markdownInner = SnooStreamViewModel.MarkdownProcessor.Process(body);
                                var isPlainText = SnooStreamViewModel.MarkdownProcessor.IsPlainText(markdownInner);
                                return Tuple.Create(markdownInner, isPlainText);
                            }
                            catch (Exception)
                            {
                                //TODO log this failure
                                return Tuple.Create<MarkdownData, bool>(null, true);
                            }
                        });

                    if (loadToken.IsCancellationRequested)
                        return;

                    if (!markdownTpl.Item2)
                    {
                        contentControl.ContentTemplate = Resources["markdownTemplate"] as DataTemplate;
                        contentControl.Content = markdownTpl.Item1.MarkdownDom;
                        
                    }
                    else if (contentControl.Content == null)
                    {
                        var textContent = (Resources["textTemplate"] as DataTemplate).LoadContent() as FrameworkElement;
                        textContent.DataContext = body;
                        contentControl.Content = textContent;
                        contentControl.MinHeight = 0;
                    }
                    LoadPhase = 3;
                }
                else
                    throw new NotImplementedException();
            }
        }
	}
}
