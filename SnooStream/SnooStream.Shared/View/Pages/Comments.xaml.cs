using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using SnooStream.Common;
using SnooStream.ViewModel;
using SnooStream.View.Controls;
using GalaSoft.MvvmLight;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace SnooStream.View.Pages
{
    public partial class Comments : SnooApplicationPage
    {
        public Comments()
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

        public override async void SetFocusedViewModel(ViewModelBase viewModel)
        {
            if (viewModel != null)
            {
                commentsView.commentsList.SelectedItem = viewModel;
                await Task.Delay(10);
                base.SetFocusedViewModel(viewModel);
                commentsView.commentsList.SafeScrollIntoView(viewModel);
            }
        }
    }
}
