using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using SnooStream.Common;
using SnooStream.ViewModel;
using Windows.UI.Xaml.Data;
using GalaSoft.MvvmLight.Messaging;
using SnooStream.Messages;
using Windows.UI.Xaml;
using SnooStream.View.Controls;
using Windows.UI.Xaml.Controls;
using GalaSoft.MvvmLight;
using Windows.UI.Xaml.Media.Animation;
using System.ComponentModel;

namespace SnooStream.View.Pages
{
    public partial class LinkRiver : SnooApplicationPage, INotifyPropertyChanged
    {
        public LinkRiver()
        {
            InitializeComponent();
#if WINDOWS_PHONE_APP
            var transition = new NavigationThemeTransition();
            transition.DefaultNavigationTransitionInfo = new ContinuumNavigationTransitionInfo();
            if (Transitions == null)
                Transitions = new TransitionCollection();

            Transitions.Add(transition);
#endif
        }

		private void linksListView_ContainerContentChanging(Windows.UI.Xaml.Controls.ListViewBase sender, Windows.UI.Xaml.Controls.ContainerContentChangingEventArgs args)
		{
			var card = args.ItemContainer.ContentTemplateRoot as CardLinkView;
            if (card != null)
            {
                if (card.PhaseLoad(sender, args))
                    args.RegisterUpdateCallback(linksListView_ContainerContentChanging);
            }
            else
            {
            }
		}

        internal bool PhaseLoad(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            var dataContext = args.Item as LinkViewModel;

            if (args.InRecycleQueue)
            {
                cancelSource.Cancel();
                cancelSource = new CancellationTokenSource();

                if (previewSection != null)
                {
                    if (previewSection.Content is CardPreviewImageControl)
                    {
                        var imageControl = previewSection.Content as CardPreviewImageControl;
                        if (imageControl.hqImageControl.Source is BitmapSource)
                        {
                            ((BitmapSource)imageControl.hqImageControl.Source).SetSource(_streamHack);
                        }
                    }
                }
            }

            if (!args.InRecycleQueue)
            {
                switch (args.Phase)
                {
                    case 0:
                        return true;
                    case 1:
                        var finishLoad2 = new Action(async () =>
                        {
                            try
                            {
                                var cancelToken = cancelSource.Token;
                                var previewControl = await ContentPreviewConverter.MakePreviewControl(DataContext as LinkViewModel, cancelToken, previewSection.Content);
                                if (!cancelToken.IsCancellationRequested)
                                {
                                    if (previewSection.Content != previewControl)
                                        previewSection.Content = previewControl;
                                }
                            }
                            catch (TaskCanceledException)
                            {
                            }
                        });
                        finishLoad2();
                        return false;
                }
            }

            return false;
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
		{
			if (DataContext is LinkRiverViewModel)
			{
				await ((LinkRiverViewModel)DataContext).Refresh(false);
                var viewModel = ((LinkRiverViewModel)DataContext).Links.FirstOrDefault();
                linksListView.SafeScrollIntoView(viewModel);
			}
		}

        public override void SetFocusedViewModel(GalaSoft.MvvmLight.ViewModelBase viewModel)
        {
            base.SetFocusedViewModel(viewModel);
            linksListView.SafeScrollIntoView(linksListView.SelectedItem = viewModel);
        }

		private void LayoutRoot_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs("FullWidth"));
		}

		public double FullWidth
		{
			get
			{
				return linksListView.ActualWidth;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}