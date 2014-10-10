using GalaSoft.MvvmLight;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Services
{
    public interface INavigationService
    {
        void NavigateToComments(CommentsViewModel viewModel);
        void NavigateToLinkRiver(LinkRiverViewModel viewModel);
		void NavigateToContentRiver(LinkRiverViewModel viewModel);
        void NavigateToMessageReply(CreateMessageViewModel viewModel);
        void NavigateToPost(PostViewModel viewModel);
        void NavigateToUpload(UploadViewModel viewModel);
        void NavigateToWeb(string url);
        void NavigateToSearch(SearchViewModel viewModel);
        void NavigateToAboutReddit(AboutRedditViewModel viewModel);
        void NavigateToAboutUser(AboutUserViewModel viewModel);
		void NavigateToContentSettings(SettingsViewModel viewModel);
		void NavigateToLockScreenSettings(SettingsViewModel viewModel);
        Task<bool> ShowPopup(ViewModelBase viewModel);
        void GoBack();

        void ValidateStates(HashSet<string> validStates);
        ViewModelBase GetState(String guid);
        void RemoveState(String guid);

        
    }
}
