﻿using GalaSoft.MvvmLight;
using SnooSharp;
using SnooStream.Common;
using SnooStream.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class SubredditRiverViewModel : ViewModelBase
    {
        public string SmallGroupNameSelector(LinkRiverViewModel viewModel)
        {
            return viewModel.IsLocal ? "local" : "subscribed";
        }

        public string LargeGroupNameSelector(LinkRiverViewModel viewModel)
        {
            return viewModel.Thing.DisplayName.Substring(0, 1);
        }

        public ObservableCollection<LinkRiverViewModel> CombinedRivers { get; private set; }
        public SubredditRiverViewModel(SubredditRiverInit initBlob)
        {
            if (initBlob != null)
            {
                
                var localSubreddits = initBlob.Pinned.Select(blob => new LinkRiverViewModel(true, blob.Thing, blob.DefaultSort, blob.Links));
                var subscribbedSubreddits = initBlob.Subscribed.Select(blob => new LinkRiverViewModel(false, blob.Thing, blob.DefaultSort, blob.Links));

                CombinedRivers = new ObservableCollection<LinkRiverViewModel>(localSubreddits.Concat(subscribbedSubreddits));
                EnsureFrontPage();
                ReloadSubscribed(false);
            }
            else
            {
                LoadWithoutInitial();
                EnsureFrontPage();
            }
            MessengerInstance.Register<UserLoggedInMessage>(this, OnUserLoggedIn);
        }

        private void OnUserLoggedIn(UserLoggedInMessage obj)
        {
            ReloadSubscribed(true);
        }

        private void EnsureFrontPage()
        {
            if (!CombinedRivers.Any((lrvm) => lrvm.Thing.Url == "/"))
            {
                CombinedRivers.Add(new LinkRiverViewModel(true, new Subreddit("/"), "hot", null));
            }

        }

        private async void LoadWithoutInitial()
        {
            CombinedRivers = new ObservableCollection<LinkRiverViewModel>();
            Listing subscribedListing = null; 
            if (SnooStreamViewModel.RedditUserState != null && !string.IsNullOrWhiteSpace(SnooStreamViewModel.RedditUserState.Username))
            {
                subscribedListing = await SnooStreamViewModel.RedditService.GetSubscribedSubredditListing() ?? await SnooStreamViewModel.RedditService.GetDefaultSubreddits();
            }
            else
            {
                subscribedListing = await SnooStreamViewModel.RedditService.GetDefaultSubreddits();
            }

            foreach (var river in subscribedListing.Data.Children.Select(thing => new LinkRiverViewModel(false, thing.Data as Subreddit, "hot", null)))
            {
                CombinedRivers.Add(river);
            }
        }

        private async void ReloadSubscribed(bool required)
        {
            if (!required)
            {
                if (!SnooStreamViewModel.SystemServices.IsLowPriorityNetworkOk)
                    return;
                else
                    await Task.Delay(10000);
            }

            var subscribedListing = await SnooStreamViewModel.RedditService.GetSubscribedSubredditListing();

            foreach (var river in subscribedListing.Data.Children.Select(thing => new LinkRiverViewModel(false, thing.Data as Subreddit, "hot", null)))
            {
                //TODO dont touch things that are already there only add/remove
                CombinedRivers.Add(river);
            }
        }
    }
}
