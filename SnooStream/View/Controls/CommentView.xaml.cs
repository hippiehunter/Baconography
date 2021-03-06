﻿using SnooStream.Services;
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
using Windows.UI.Xaml.Controls.Primitives;

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
                if (((CommentViewModel)args.Item).IsEditing)
                {
                    var editContent = (Resources["editingTemplate"] as DataTemplate).LoadContent() as FrameworkElement;
                    editContent.DataContext = ((CommentViewModel)DataContext).ReplyViewModel;
                    contentControl.Content = editContent;
                    contentControl.MinHeight = 0;
                    return false;
                }
                else
                {
                    contentControl.ContentTemplate = null;
                    contentControl.Content = null;
                    var body = ((CommentViewModel)args.Item).Body ?? "";
                    contentControl.MinHeight = Math.Max(25, body.Length / 2);
                    args.Handled = true;
                    LoadPhase = 1;
                    return true;
                }
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

        internal async void FinishPhaseLoad(CommentViewModel viewModel)
        {
            try
            {
                var body = viewModel.Body;
                var loadToken = LoadCancelSource.Token;
                var markdown = await SnooStreamViewModel.SystemServices.RunAsyncIdle(() =>
                {
                    try
                    {
                        var markdownInner = SnooStreamViewModel.MarkdownProcessor.Process(body);
						return markdownInner;
                    }
                    catch (Exception)
                    {
                        //TODO log this failure
						return null;
                    }
                }, loadToken);


                if (loadToken.IsCancellationRequested)
                    return;

                contentControl.ContentTemplate = Resources["markdownTemplate"] as DataTemplate;
				contentControl.Content = markdown.MarkdownDom;
                
                LoadPhase = 3;
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }

        internal void Phase2Load(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (!args.InRecycleQueue)
            {
                if (args.Item is CommentViewModel)
                {
                    args.Handled = true;
                    FinishPhaseLoad(args.Item as CommentViewModel);
                }
                else
                    throw new NotImplementedException();
            }
        }

        private void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext != null && DataContext is CommentViewModel)
            {
                ((CommentViewModel)DataContext).PropertyChanged -= CommentView_PropertyChanged;
            }

            if (args.NewValue is CommentViewModel)
            {
                ((CommentViewModel)DataContext).PropertyChanged += CommentView_PropertyChanged;
            }
        }

        private void CommentView_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsEditing")
            {
				contentControl.ContentTemplate = null;
                if (((CommentViewModel)DataContext).IsEditing)
                {
                    var editContent = (Resources["editingTemplate"] as DataTemplate).LoadContent() as FrameworkElement;
                    contentControl.Content = editContent;
					editContent.DataContext = ((CommentViewModel)DataContext).ReplyViewModel;
                    contentControl.MinHeight = 0;
                }
                else
                {
                    contentControl.Content = null;
                    FinishPhaseLoad(DataContext as CommentViewModel);
                }
            }
        }

        private void Button_Holding(object sender, Windows.UI.Xaml.Input.HoldingRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private void Button_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            ((CommentViewModel)DataContext).MinimizeCommand.Execute(null);
        }

    }
}
