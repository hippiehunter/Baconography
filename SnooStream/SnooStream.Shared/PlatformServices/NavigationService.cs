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
using Newtonsoft.Json;
using GalaSoft.MvvmLight;
using SnooStream.ViewModel.Popups;
using Windows.UI.Popups;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace SnooStream.PlatformServices
{
    public class NavigationService : INavigationService
    {
        Frame _frame;
        NavigationStateUtility _navState;
		SnooStreamViewModel _rootContext;

        public NavigationService(Frame frame, SnooStreamViewModel rootContext)
        {
			_rootContext = rootContext;
            _frame = frame;
        }

		public void Finish(string existingState)
		{
			try
			{
				var serializationTpl = existingState != null ? JsonConvert.DeserializeObject<Tuple<string, string, DateTime>>(existingState) : Tuple.Create<string, string, DateTime>(null, null, DateTime.Now);
				var navAge = (DateTime.Now - serializationTpl.Item3).TotalHours;
				_navState = new NavigationStateUtility(navAge < 8 ? serializationTpl.Item1 : null, _rootContext);
				if (serializationTpl.Item2 != null && navAge < 8)
				{
					_frame.SetNavigationState(serializationTpl.Item2);
				}
			}
			catch
			{
				_navState = new NavigationStateUtility("", _rootContext);
			}
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
			_frame.Navigate(typeof(ComposeMessageView), "state=" + _navState.AddState(viewModel));
        }

        public void NavigateToPost(PostViewModel viewModel)
        {
			_frame.Navigate(typeof(ComposePost), "state=" + _navState.AddState(viewModel));
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

        public async Task ShowPopup(ViewModelBase viewModel, object elementTarget)
        {
            if (viewModel is CommandViewModel)
            {
                var popup = new PopupMenu();
                foreach( var command in ((CommandViewModel)viewModel).Commands)
                {
                    popup.Commands.Add(new UICommand(command.DisplayText, (u) => command.Command.Execute(null)));
                }
                Rect selection = Window.Current.Bounds;
                if (elementTarget != null && elementTarget is RoutedEventArgs)
                {
                    var sourceElement = (elementTarget as RoutedEventArgs).OriginalSource;
                    GeneralTransform buttonTransform = ((FrameworkElement)sourceElement).TransformToVisual(null);
                    Point point = buttonTransform.TransformPoint(new Point());
                    selection = new Rect(point, new Size(((FrameworkElement)sourceElement).ActualWidth, ((FrameworkElement)sourceElement).ActualHeight));
                }
                await popup.ShowForSelectionAsync(selection, Placement.Below);
            }
            else
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
			var frameState = _frame.GetNavigationState();
            var navState = _navState.DumpState();
			return JsonConvert.SerializeObject(Tuple.Create(navState, frameState, DateTime.Now));
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

        public void NavigateToConversation(string conversationId)
        {
            _frame.Navigate(typeof(Conversation), "state=" + _navState.AddState(new ConversationViewModel(_rootContext.SelfStream.Groups[conversationId], _rootContext.SelfStream)));
        }

        public void ValidateStates(HashSet<string> validStates)
        {
            _navState.ValidateParameters(validStates);
        }

		public void NavigateToContentRiver(IHasLinks viewModel)
		{
			_frame.Navigate(typeof(ContentRiver), "state=" + _navState.AddState(viewModel as ViewModelBase));
		}


		public void PushVisualState(object sender, string visualState)
		{
			var snooApplicationPage = _frame.Content as SnooApplicationPage;
			snooApplicationPage.PushNavState(sender, visualState);
		}

		public void PopVisualState()
		{
			var snooApplicationPage = _frame.Content as SnooApplicationPage;
			snooApplicationPage.PopNavState();
		}

        
    }
}
