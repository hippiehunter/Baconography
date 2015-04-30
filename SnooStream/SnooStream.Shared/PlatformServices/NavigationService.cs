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
using Windows.UI.Xaml.Input;
using System.Threading;

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
			_frame.Navigate(typeof(LinkRiver), "state=" + _navState.AddState(viewModel));
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
            _frame.Navigate(typeof(AboutSubreddit), "state=" + _navState.AddState(viewModel));
        }

		public void NavigateToContentSettings(SettingsViewModel viewModel)
		{
			_frame.Navigate(typeof(ContentSettings), "state=" + _navState.AddState(viewModel));
		}

		public void NavigateToLockScreenSettings(SettingsViewModel viewModel)
		{
			_frame.Navigate(typeof(LockScreenSettings), "state=" + _navState.AddState(viewModel));
		}

        public Task ShowPopup(ViewModelBase viewModel, object elementTarget, CancellationToken abortToken)
        {
            if (viewModel is CommandViewModel)
            {
                var popup = new MenuFlyout();
                foreach( var command in ((CommandViewModel)viewModel).Commands)
                {
                    popup.Items.Add(new MenuFlyoutItem { Text = command.DisplayText, Command = command.Command });
                }
                var windowWidth = Window.Current.Bounds.Width;
                var windowHeight = Window.Current.Bounds.Height;
                if (elementTarget != null && elementTarget is RoutedEventArgs)
                {
                    var sourceElement = (elementTarget as RoutedEventArgs).OriginalSource as FrameworkElement;
                    TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
                    Action onAbort = () => { popup.Hide(); };
                    var cancelationTokenRegistration = abortToken.Register(onAbort);
                    popup.Closed += (sender, args) =>
                    {
                        completionSource.TrySetResult(true);
                        cancelationTokenRegistration.Dispose();
                    };
                    
                    popup.Placement = Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom;
                    popup.ShowAt(sourceElement);
                    return completionSource.Task;
                }
                else
                    throw new NotImplementedException();
            }
            else if (viewModel is InputViewModel)
            {
                if (elementTarget != null && elementTarget is RoutedEventArgs)
                {
                    var inputViewModel = viewModel as InputViewModel;
                    var popup = new Flyout();
#if WINDOWS_PHONE_APP
                    var inputBox = new AutoSuggestBox { ItemsSource = inputViewModel.SearchOptions, Text = inputViewModel.InputValue };
                    inputBox.TextChanged += (sender, args) =>
                        {
                            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                                inputViewModel.InputValue = sender.Text;
                        };
                    inputBox.SuggestionChosen += (sender, args) =>
                        {
                            inputViewModel.InputValue = sender.Text;
                            popup.Hide();
                        };
#else
                    var inputBox = new TextBox { Text = inputViewModel.InputValue };
#endif
                    bool oked = false;
                    inputBox.KeyDown += (sender, args) =>
                        {
                            if (args.Key == VirtualKey.Enter)
                            {
                                oked = true;
                                popup.Hide();
                            }
                        };
                    var stackPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };
                    stackPanel.Children.Add(new TextBlock { Text = inputViewModel.Prompt });
                    stackPanel.Children.Add(inputBox);
                    var buttons = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch, Orientation = Orientation.Horizontal};
                    stackPanel.Children.Add(stackPanel);
                    var okButton = new Button { Content = "Ok"};
                    var cancelButton = new Button { Content = "Cancel"};
                    buttons.Children.Add(okButton);
                    buttons.Children.Add(cancelButton);
                    popup.Content = stackPanel;
                    
                    okButton.Tapped += (sender, args) =>
                        {
                            oked = true;
                            popup.Hide();
                        };

                    cancelButton.Tapped += (sender, args) =>
                        {
                            oked = false;
                            popup.Hide();
                        };

                    TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
                    popup.Closed += (sender, args) =>
                        {
                            if (inputViewModel.Dismissed != null && oked)
                                inputViewModel.Dismissed.Execute(string.IsNullOrWhiteSpace(inputBox.Text) ? "pinned" : inputBox.Text);
                            completionSource.TrySetResult(true);
                        };
                    popup.Placement = Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom;
                    popup.ShowAt(((RoutedEventArgs)elementTarget).OriginalSource as FrameworkElement);
                    return completionSource.Task;
                }
                else
                    throw new NotImplementedException();
            }
            else if(viewModel is OperationCancellationViewModel)
            {
                var cancelViewModel = viewModel as OperationCancellationViewModel;
                var popup = new Flyout();
                popup.Content = new CancelOperationDialog { DataContext = cancelViewModel };
                popup.Placement = Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom;

                TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
                popup.Closed += (sender, args) =>
                {
                    completionSource.TrySetResult(true);
                };

                popup.ShowAt(Window.Current.Content as FrameworkElement);
                return completionSource.Task;
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
            _frame.Navigate(typeof(AboutUser), "state=" + _navState.AddState(viewModel));
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

        public void NavigateToMessageReply(CreateMessageViewModel viewModel)
        {
            _frame.Navigate(typeof(Conversation), "state=" + _navState.AddState(new ConversationViewModel(null, _rootContext.SelfStream, viewModel)));
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

        public void NavigateToMultiRedditManagement(LinkRiverViewModel viewModel)
        {
            throw new NotImplementedException();
        }

        public void NavigateToSubredditCategorizer(LinkRiverViewModel viewModel)
        {
            throw new NotImplementedException();
        }


        public void NavigateToOAuthLanding(LoginViewModel loginViewModel)
        {
            _frame.Navigate(typeof(OAuthLanding), "state=" + _navState.AddState(loginViewModel));
        }


        public void NavigateToSubredditModeration(SubredditModerationViewModel viewModel)
        {
            throw new NotImplementedException();
        }
    }
}
