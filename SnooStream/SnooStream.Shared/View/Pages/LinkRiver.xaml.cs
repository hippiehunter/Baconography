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
using SnooStream.View.Controls.CardView;

namespace SnooStream.View.Pages
{
    public partial class LinkRiver : SnooApplicationPage
    {
        public LinkRiver()
        {
            InitializeComponent();
        }
		protected override async void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			if (DataContext is LinkRiverViewModel)
			{
				if (((LinkRiverViewModel)DataContext).CurrentSelected != null)
				{
					await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
						() => linksListView.ScrollIntoView(((LinkRiverViewModel)DataContext).CurrentSelected));
				}
			}
				
		}

		protected override void OnNavigatedFrom(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);

			if(e.NavigationMode == Windows.UI.Xaml.Navigation.NavigationMode.Back)
			{
				((LinkRiverViewModel)DataContext).CurrentSelected = null;
				linksListView.ScrollIntoView(null);
			}
		}

		private void linksListView_ContainerContentChanging(Windows.UI.Xaml.Controls.ListViewBase sender, Windows.UI.Xaml.Controls.ContainerContentChangingEventArgs args)
		{
			var card = args.ItemContainer.ContentTemplateRoot as CardLinkView;
			if(card != null)
				card.PhaseLoad(sender, args);
		}

		private void Sort_Click(object sender, RoutedEventArgs e)
		{
			double height = 480;
			double width = 325;

			if (LayoutRoot.ActualHeight <= 480)
				height = LayoutRoot.ActualHeight;

			sortPopup.Height = height;
			sortPopup.Width = width;

			var linkRiverViewModel = DataContext as LinkRiverViewModel;
			if (linkRiverViewModel == null)
				return;


			var child = sortPopup.Child as SelectSortTypeView;
			if (child == null)
				child = new SelectSortTypeView();
			child.SortOrder = linkRiverViewModel.Sort;
			child.Height = height;
			child.Width = width;
			child.button_ok.Click += (object buttonSender, RoutedEventArgs buttonArgs) =>
			{
				linkRiverViewModel.SetSort(child.SortOrder);
				PopNavState();
			};

			child.button_cancel.Click += (object buttonSender, RoutedEventArgs buttonArgs) =>
			{
				PopNavState();
			};

			sortPopup.Child = child;
			PushNavState(this, "ShowSortPopup");
		}

		private async void Refresh_Click(object sender, RoutedEventArgs e)
		{
			if (DataContext is LinkRiverViewModel)
			{
				await ((LinkRiverViewModel)DataContext).Refresh(false);
				linksListView.ScrollIntoView(((LinkRiverViewModel)DataContext).Links.FirstOrDefault());
			}
		}

        private void CardLinkView_MoreClick(object sender, EventArgs e)
        {
            double height = 480;
            double width = 325;

            if (LayoutRoot.ActualHeight <= 480)
                height = LayoutRoot.ActualHeight;

            morePopup.Height = height;
            morePopup.Width = width;

            var linkViewModel = ((FrameworkElement)sender).DataContext as LinkViewModel;
            if (linkViewModel == null)
                return;


            var child = new LinkMoreControl { DataContext = linkViewModel };
            child.Tapped += LinkMoreItem_Tapped;
            child.Height = height;
            child.Width = width;

            morePopup.Child = child;
            PushNavState(this, "ShowMorePopup");
        }

        private void LinkMoreItem_Tapped(object sender, EventArgs e)
        {
            PopNavState();
            morePopup.Child = null;
        }
    }
}