using GalaSoft.MvvmLight;
using Newtonsoft.Json;
using SnooSharp;
using SnooStream.Common;
using SnooStream.Messages;
using SnooStream.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
	public class ModStreamViewModel : ViewModelBase
	{
		public class ModActivityLoader : IIncrementalCollectionLoader<ViewModelBase>
		{
			ModStreamViewModel _modStream;
            ActivityGroupViewModel.ActivityAggregate _activityAggregate;
			public ModActivityLoader(ModStreamViewModel modStream)
			{
				_modStream = modStream;
			}

			public Task AuxiliaryItemLoader(IEnumerable<ViewModelBase> items, int timeout)
			{
				return Task.FromResult(true);
			}

			public bool IsStale
			{
				get { return _modStream.LastRefresh == null || (DateTime.Now - _modStream.LastRefresh.Value).TotalMinutes > 30; }
			}

			public bool HasMore()
			{
				var anyQueue = _modStream.Subreddits.Any(sr => sr.Enabled && !string.IsNullOrWhiteSpace(sr.OldestQueue));
				//TODO deal with active subreddit mods
				return SnooStreamViewModel.RedditUserState.IsMod && (_modStream.OldestModMail != null || anyQueue);
			}

			public async Task<IEnumerable<ViewModelBase>> LoadMore()
			{
				await _modStream.PullOlder();
				return Enumerable.Empty<ViewModelBase>();
			}

			public async Task Refresh(System.Collections.ObjectModel.ObservableCollection<ViewModelBase> current, bool onlyNew)
			{
				await _modStream.PullNew(false, !onlyNew);
			}

			public string NameForStatus
			{
				get { return "mod activitie"; }
			}

			public void Attach(System.Collections.ObjectModel.ObservableCollection<ViewModelBase> targetCollection)
			{
                _activityAggregate = new ActivityGroupViewModel.ActivityAggregate(_modStream.Groups, targetCollection);
			}
		}

		public ModStreamViewModel()
		{
			//load up the activities
			Groups = new ObservableSortedUniqueCollection<string, ActivityGroupViewModel>(new ActivityGroupViewModel.ActivityAgeComparitor());
			Activities = SnooStreamViewModel.SystemServices.MakeIncrementalLoadCollection(new ModActivityLoader(this), 100);
			Subreddits = new List<SubredditMod>();
			DisabledModeration = new List<string>();
            if (IsLoggedIn)
			{
				InitialLoad();
            }
		}

		private async void InitialLoad()
		{
            await Task.Delay(5000);
			await PullNew(true, true);
		}

		public async void OnUserLoggedIn(UserLoggedInMessage obj)
		{
            SnooStreamViewModel.ActivityManager.OAuth = SnooStreamViewModel.RedditUserState != null && SnooStreamViewModel.RedditUserState.OAuth != null ?
                    JsonConvert.SerializeObject(SnooStreamViewModel.RedditUserState) : "";
			SnooStreamViewModel.ActivityManager.CanStore = SnooStreamViewModel.RedditUserState != null && SnooStreamViewModel.RedditUserState.IsDefault;
            RaisePropertyChanged("IsLoggedIn");
			RaisePropertyChanged("Activities");
			Groups.Clear();
            if (IsLoggedIn)
            {
				await PullNew(false, true);
            }
			
		}

		public bool IsLoggedIn
		{
			get
			{
				return !String.IsNullOrWhiteSpace(SnooStreamViewModel.RedditService.CurrentUserName) && 
                    SnooStreamViewModel.RedditUserState.OAuth != null &&
                    !String.IsNullOrWhiteSpace(SnooStreamViewModel.RedditUserState.OAuth.RefreshToken);
			}
		}

		public static IEnumerable<Thing> DumpThings(IEnumerable<ActivityGroupViewModel> activities)
		{
			List<Thing> things = new List<Thing>();
			foreach (var group in activities)
			{
				group.DumpThings(things);
			}
			return things;
		}

		public class SubredditMod
		{
			public string OldestQueue {get; set;}
			public string Subreddit {get; set;}
			public bool Enabled {get; set;}
		}

		private string OldestModMail { get; set; }
		private List<SubredditMod> Subreddits {get; set;}
		private List<string> DisabledModeration { get; set; }
		public DateTime? LastRefresh { get; set; }
		public ObservableSortedUniqueCollection<string, ActivityGroupViewModel> Groups { get; private set; }
		public ObservableCollection<ViewModelBase> Activities { get; private set; }
        public static Dictionary<string, ActivityViewModel> ActivityLookup = new Dictionary<string, ActivityViewModel>();
		public async Task PullNew(bool userInitiated, bool appStart)
        {
            LastRefresh = DateTime.Now;
            if (!IsLoggedIn)
                throw new InvalidOperationException("User must be logged in to do this");


			if (SnooStreamViewModel.RedditUserState.IsMod)
			{
				//update moderator listing
				var modSubs = await SnooStreamViewModel.RedditService.GetModeratorSubredditListing(CancellationToken.None);
				//load mod mail
				//var modMail = await SnooStreamViewModel.RedditService.GetModMail(null);

				//OldestModMail = ActivityGroupViewModel.ProcessListing(Groups, modMail, null);
				//load mod queue for each subreddit
				foreach (var modSub in modSubs.Data.Children)
				{
                    try
                    {
                        if (modSub.Data is Subreddit)
                        {
                            var subredditName = Reddit.MakePlainSubredditName(((Subreddit)modSub.Data).Url);
                            if (!DisabledModeration.Contains(subredditName))
                            {
                                var modQueue = await SnooStreamViewModel.RedditService.GetModQueue(subredditName, 20);
                                var newModSub = new SubredditMod
                                {
                                    Enabled = true,
                                    Subreddit = subredditName,
                                    OldestQueue = ActivityGroupViewModel.ProcessListing(Groups, modQueue, null, true)
                                };

                                Subreddits.Add(newModSub);
                            }
                            else
                            {
                                Subreddits.Add(new SubredditMod { Enabled = false, OldestQueue = null, Subreddit = subredditName });
                            }
                        }
                    }
                    catch
                    {
                    }
				}
			}
        }

		public void AddMessageActivity(string targetUser, string topic, string contents)
		{
			ActivityGroupViewModel.ProcessThing(Groups, new Thing
			{
				Kind = "t4",
				Data = new Message
					{
						Author = SnooStreamViewModel.RedditUserState.Username,
						Body = contents,
						Destination = targetUser,
						Subject = topic,
						Created = DateTime.Now,
						CreatedUTC = DateTime.UtcNow
					}
			}, true);
		}

		public async Task PullOlder()
		{
			LastRefresh = DateTime.Now;

			if (!IsLoggedIn)
				throw new InvalidOperationException("User must be logged in to do this");

			if (SnooStreamViewModel.RedditUserState.IsMod)
			{
				OldestModMail = await PullActivity(OldestModMail, "mod mail", string.Format(Reddit.MailBaseUrlFormat, "moderator"));
				//foreach mod sub whos Oldest Queue isnt null grab more
				foreach (var subMod in Subreddits)
				{
					if (subMod.Enabled)
					{
						subMod.OldestQueue = await PullActivity(subMod.OldestQueue, "mod queue", string.Format(Reddit.SubredditAboutBaseUrlFormat, subMod.Subreddit, "modqueue"));
					}
				}
			}
		}

        private async Task<string> PullActivity(string oldest, string displayName, string uriFormat)
        {
            Listing activity = null;
            if (!string.IsNullOrWhiteSpace(oldest))
            {
                await SnooStreamViewModel.NotificationService.Report("getting additional " + displayName, async () =>
                {
					try
					{
						activity = await SnooStreamViewModel.RedditService.GetAdditionalFromListing(uriFormat, oldest, null);
					}
					catch //make this silent, we get failures here when there arent any more items
					{
						oldest = null;
					}
                });

				if (activity != null)
				{
					return ActivityGroupViewModel.ProcessListing(Groups, activity, oldest);
				}
				else
					return oldest;
            }
            else
                return oldest;
        }

		public async Task MaybeRefresh()
		{
			if (!string.IsNullOrWhiteSpace(SnooStreamViewModel.RedditUserState.Username))
			{
				if (LastRefresh == null || (DateTime.Now - LastRefresh.Value).TotalMinutes > 30)
					await Refresh(false);
			}
		}

		public async Task Refresh(bool onlyNew)
		{
			if (!onlyNew)
			{
				Groups.Clear();
				await PullNew(true, false);
			}
		}
	}
}
