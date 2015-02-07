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

namespace SnooStream.View.Pages
{
    public partial class LinkRiver : SnooApplicationPage
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
    }
}