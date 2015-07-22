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
using System.Threading.Tasks;
using SnooStream.Converters;

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
            if (PhaseLoad(sender, args))
                args.RegisterUpdateCallback(linksListView_ContainerContentChanging);
		}

        internal bool PhaseLoad(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            var dataContext = args.Item as LinkViewModel;
            var button = ((FrameworkElement)args.ItemContainer.ContentTemplateRoot).FindName("previewSection") as Button;
            if (args.InRecycleQueue)
            {
                dataContext.Content.CancelLoad();
                button.Content = null;
            }
            else
            {
                switch (args.Phase)
                {
                    case 0:
                        var result = Preview.LoadLinkPreview(dataContext.Content);
                        button.Content = result;
                        return true;
                    case 1:
                        var preview = button.Content as Preview;
                        var finishLoad2 = new Action(async () =>
                        {
                            try
                            {
                                await Task.Run(async () =>
                                {
                                    await preview.FinishLoad(dataContext.Content.CancelToken);
                                });
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