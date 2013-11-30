﻿using GalaSoft.MvvmLight;
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
        void NavigateToLinkStream(LinkStreamViewModel viewModel);
        void NavigateToMessageReply(CreateMessageViewModel viewModel);
        void NavigateToPost(PostViewModel viewModel);
        void NavigateToUpload(UploadViewModel viewModel);
        void NavigateToSearch(SearchViewModel viewModel);
        void NavigateToAboutReddit(AboutRedditViewModel viewModel);
        void NavigateToSettingsWithPreview(SettingsViewModel viewModel);
        Task<bool> ShowPopup(ViewModelBase viewModel);
        void GoBack();
    }
}
