using SnooStream.Services;
using SnooStream.ViewModel;
using SnooStreamWP8.Common;
using SnooStreamWP8.View.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Windows.System;

namespace SnooStreamWP8.PlatformServices
{
    public class NavigationService : INavigationService
    {
        Frame _frame;
        NavigationStateUtility _navState;
		SnooStreamViewModel _rootContext;

        public NavigationService(Frame frame, string existingState, SnooStreamViewModel rootContext)
        {
			_rootContext = rootContext;
            _frame = frame;
			_navState = new NavigationStateUtility(existingState, _rootContext);
        }

        public void NavigateToComments(CommentsViewModel viewModel)
        {
            var url = string.Format("/View/Pages/Comments.xaml?state={0}", _navState.AddState(viewModel));
            _frame.Navigate(new Uri(url, UriKind.Relative));
        }

        public void NavigateToLinkRiver(LinkRiverViewModel viewModel)
        {
            //if the current frame is already the hub just change pages and dont introduce any back stack
            if (_frame.Content is LinkRiver)
            {
				var hub = _frame.Content as LinkRiver;
                var ssvm = hub.DataContext as SnooStreamViewModel;
                ssvm.SubredditRiver.SelectSubreddit(viewModel);
            }
            else
            {
                var url = string.Format("/View/Pages/LinkRiver.xaml?state={0}", _navState.AddState(viewModel));
                _frame.Navigate(new Uri(url, UriKind.Relative));
            }
        }

        public void NavigateToLinkStream(LinkStreamViewModel viewModel)
        {
            var url = string.Format("/View/Pages/LinkStream.xaml?state={0}", _navState.AddState(viewModel));
            _frame.Navigate(new Uri(url, UriKind.Relative));
        }

        public void NavigateToMessageReply(CreateMessageViewModel viewModel)
        {
            throw new NotImplementedException();
        }

        public void NavigateToPost(PostViewModel viewModel)
        {
            throw new NotImplementedException();
        }

        public void NavigateToUpload(UploadViewModel viewModel)
        {
            throw new NotImplementedException();
        }

        public void NavigateToSearch(SearchViewModel viewModel)
        {
            throw new NotImplementedException();
        }

        public void NavigateToAboutReddit(AboutRedditViewModel viewModel)
        {
            throw new NotImplementedException();
        }

		public void NavigateToContentSettings(SettingsViewModel viewModel)
		{
			var url = string.Format("/View/Pages/ContentSettings.xaml?state={0}", _navState.AddState(viewModel));
			_frame.Navigate(new Uri(url, UriKind.Relative));
		}

		public void NavigateToLockScreenSettings(SettingsViewModel viewModel)
		{
            var url = string.Format("/View/Pages/LockScreenSettings.xaml?state={0}", _navState.AddState(viewModel));
            _frame.Navigate(new Uri(url, UriKind.Relative));
		}

        public Task<bool> ShowPopup(GalaSoft.MvvmLight.ViewModelBase viewModel)
        {
            throw new NotImplementedException();
        }

        public void GoBack()
        {
            try
            {
                _frame.GoBack();
            }
            catch
            {
                //whatever the failure was we need to ignore it, it was an invalid request and
                //msdn seems to suggest that this is not really a bug in user code
            }
        }


        public GalaSoft.MvvmLight.ViewModelBase GetState(string guid)
        {
            return _navState[guid];
        }

        public void RemoveState(string guid)
        {
            _navState.RemoveState(guid);
        }

        public string DumpState()
        {
            return _navState.DumpState();
        }


        public void NavigateToAboutUser(AboutUserViewModel viewModel)
        {
            var url = string.Format("/View/Pages/User.xaml?state={0}", _navState.AddState(viewModel));
            _frame.Navigate(new Uri(url, UriKind.Relative));
        }


        public async void NavigateToWeb(string url)
        {
            try
            {
                await Launcher.LaunchUriAsync(new Uri(url));
            }
            catch (AccessViolationException)
            {
                //this is platform sillyness when somehow someone triggers this twice it crashes the app
            }
            catch(UriFormatException)
            { 
                //TODO message box this?
            }
        }
	}
}
