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
		protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			if (e.NavigationMode == Windows.UI.Xaml.Navigation.NavigationMode.Back)
			{
				linksListView.ScrollIntoView(((LinkRiverViewModel)DataContext).LinksViewSource.View.CurrentItem);
			}
		}

		private void linksListView_ContainerContentChanging(Windows.UI.Xaml.Controls.ListViewBase sender, Windows.UI.Xaml.Controls.ContainerContentChangingEventArgs args)
		{
			var card = args.ItemContainer.ContentTemplateRoot as CardLinkView;
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
    }
}