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

namespace SnooStream.View.Pages
{
    public partial class LinkRiver : SnooApplicationPage
    {
        public LinkRiver()
        {
            InitializeComponent();
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
				linksListView.ScrollIntoView(((LinkRiverViewModel)DataContext).Links.FirstOrDefault());
			}
		}

        public override void SetFocusedViewModel(GalaSoft.MvvmLight.ViewModelBase viewModel)
        {
            base.SetFocusedViewModel(viewModel);
            linksListView.ScrollIntoView(linksListView.SelectedItem = viewModel);
        }
    }
}