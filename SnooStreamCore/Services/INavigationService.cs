using GalaSoft.MvvmLight;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.Services
{
    public interface INavigationService
    {
        void NavigateToComments(CommentsViewModel viewModel);
        void NavigateToLinkRiver(LinkRiverViewModel viewModel);
		void NavigateToContentRiver(IHasLinks viewModel);
        void NavigateToConversation(string conversationId);
        void NavigateToMessageReply(CreateMessageViewModel viewModel);
        void NavigateToPost(PostViewModel viewModel);
        void NavigateToUpload(UploadViewModel viewModel);
        void NavigateToWeb(string url);
        void NavigateToSearch(SearchViewModel viewModel);
        void NavigateToAboutReddit(AboutRedditViewModel viewModel);
        void NavigateToMultiRedditManagement(LinkRiverViewModel viewModel);
        void NavigateToSubredditCategorizer(LinkRiverViewModel viewModel);
        void NavigateToAboutUser(AboutUserViewModel viewModel);
		void NavigateToContentSettings(SettingsViewModel viewModel);
		void NavigateToLockScreenSettings(SettingsViewModel viewModel);
        void NavigateToOAuthLanding(LoginViewModel loginViewModel);
        Task ShowPopup(ViewModelBase viewModel, object elementTarget, CancellationToken abortToken);
		void PushVisualState(object sender, string visualState);
		void PopVisualState();
        void GoBack();

        void ValidateStates(HashSet<string> validStates);
        ViewModelBase GetState(String guid);
        void RemoveState(String guid);
        
    }
}
