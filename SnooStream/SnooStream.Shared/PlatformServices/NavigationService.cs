using SnooStream.Services;
using SnooStream.ViewModel;
using SnooStream.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml.Controls;
using SnooStream.View.Pages;
using SnooStream.View.Controls;
using GalaSoft.MvvmLight.Messaging;
using SnooStream.Messages;

namespace SnooStream.PlatformServices
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
            _frame.Navigate(typeof(Comments), "state=" + _navState.AddState(viewModel));
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
				_frame.Navigate(typeof(LinkRiver), "state=" + _navState.AddState(viewModel));
            }
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
			_frame.Navigate(typeof(ContentSettings), "state=" + _navState.AddState(viewModel));
		}

		public void NavigateToLockScreenSettings(SettingsViewModel viewModel)
		{
			_frame.Navigate(typeof(LockScreenSettings), "state=" + _navState.AddState(viewModel));
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
			throw new NotImplementedException();
        }


        public async void NavigateToWeb(string url)
        {
            try
            {
                await Launcher.LaunchUriAsync(new Uri(url));
            }
			catch(Exception)
            { 
                //TODO message box this?
            }
        }


        public void ValidateStates(HashSet<string> validStates)
        {
            _navState.ValidateParameters(validStates);
        }

		public void NavigateToContentRiver(LinkRiverViewModel viewModel)
		{
			_frame.Navigate(typeof(ContentRiver), "state=" + _navState.AddState(viewModel));
		}
	}
}
