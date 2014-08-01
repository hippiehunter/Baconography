using GalaSoft.MvvmLight;
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
        static public string SmallGroupNameSelector(LinkRiverViewModel viewModel)
        {
            return viewModel.IsLocal ? "favorites" : "subscribed";
        }

        static public string LargeGroupNameSelector(LinkRiverViewModel viewModel)
        {
            return viewModel.Thing.DisplayName.Substring(0, 1);
        }
        
        public ObservableCollection<LinkRiverViewModel> CombinedRivers { get; private set; }
        public LinkRiverViewModel SelectedRiver { get; private set; }
        public string SearchString { get; set; }
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
            SelectedRiver = CombinedRivers.FirstOrDefault() ?? new LinkRiverViewModel(true, new Subreddit("/"), "hot", null);
            MessengerInstance.Register<UserLoggedInMessage>(this, OnUserLoggedIn);
        }

        public void SelectSubreddit(LinkRiverViewModel viewModel)
        {
            SelectedRiver = viewModel;
            RaisePropertyChanged("SelectSubreddit");
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
                subscribedListing.Data.Children.Insert(0, new Thing { Data = new Subreddit { Name = "gifs", DisplayName = "gifs", Url = "/r/gifs", Title = "gifs" } });
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
				if (SnooStreamViewModel.SystemServices == null)
					await Task.Delay(1000);
				//dump out something is very wrong
				if (SnooStreamViewModel.SystemServices == null)
					return;

                if (!SnooStreamViewModel.SystemServices.IsLowPriorityNetworkOk)
                    return;
                else
                    await Task.Delay(10000);
            }

            var subscribedListing = await SnooStreamViewModel.RedditService.GetSubscribedSubredditListing();

			var newRivers = new Dictionary<string, Subreddit>();
			foreach (var subreddit in subscribedListing.Data.Children)
			{
				if(subreddit.Data is Subreddit && !newRivers.ContainsKey(((Subreddit)subreddit.Data).Id))
					newRivers.Add(((Subreddit)subreddit.Data).Id, ((Subreddit)subreddit.Data));
			}

			var missingRivers = new List<LinkRiverViewModel>();
			var existingRivers = new Dictionary<string, LinkRiverViewModel>();
			foreach (var river in CombinedRivers)
			{
				//ignore the frontpage
				if (string.IsNullOrWhiteSpace(river.Thing.Id))
					continue;

				if (!existingRivers.ContainsKey(river.Thing.Id))
					existingRivers.Add(river.Thing.Id, river);

				if(!river.IsLocal && !newRivers.ContainsKey(river.Thing.Id))
					missingRivers.Add(river);
				else if(newRivers.ContainsKey(river.Thing.Id))
					river.Thing = newRivers[river.Thing.Id];
			}

			
			foreach (var subredditTpl in newRivers)
			{
				if(!existingRivers.ContainsKey(subredditTpl.Key))
					CombinedRivers.Add(new LinkRiverViewModel(false, subredditTpl.Value,  "hot", null));
			}

			foreach (var missingRiver in missingRivers)
			{
				CombinedRivers.Remove(missingRiver);
			}
        }

		internal SubredditRiverInit Dump()
		{
			return new SubredditRiverInit
			{
				Pinned = CombinedRivers
					.Where(vm => vm.IsLocal)
					.Select(vm => vm.Dump())
					.ToList(),
				Subscribed = CombinedRivers
					.Where(vm => !vm.IsLocal)
					.Select(vm => vm.Dump())
					.ToList(),
			};
		}
	}
}
