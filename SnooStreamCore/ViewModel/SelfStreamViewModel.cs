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
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
	public class SelfStreamViewModel : ViewModelBase, IRefreshable
	{
		public class SelfActivityLoader : IIncrementalCollectionLoader<ViewModelBase>
		{
			SelfStreamViewModel _selfStream;
            ActivityGroupViewModel.ActivityAggregate _activityAggregate;
			public SelfActivityLoader(SelfStreamViewModel selfStream)
			{
				_selfStream = selfStream;
			}

			public Task AuxiliaryItemLoader(IEnumerable<ViewModelBase> items, int timeout)
			{
				return Task.FromResult(true);
			}

			public bool IsStale
			{
				get { return _selfStream.LastRefresh == null || (DateTime.Now - _selfStream.LastRefresh.Value).TotalMinutes > 30; }
			}

			public bool HasMore()
			{
				return _selfStream.OldestActivity != null ||
					_selfStream.OldestMessage != null ||
					_selfStream.OldestSentMessage != null;
			}

			public async Task<IEnumerable<ViewModelBase>> LoadMore()
			{
				await _selfStream.PullOlder();
				return Enumerable.Empty<ViewModelBase>();
			}

			public async Task Refresh(System.Collections.ObjectModel.ObservableCollection<ViewModelBase> current, bool onlyNew)
			{
				await _selfStream.PullNew(false, !onlyNew);
			}

			public string NameForStatus
			{
				get { return "activitie"; }
			}

			public void Attach(System.Collections.ObjectModel.ObservableCollection<ViewModelBase> targetCollection)
			{
                _activityAggregate = new ActivityGroupViewModel.ActivityAggregate(_selfStream.Groups, targetCollection);
			}
		}

		public SelfStreamViewModel()
		{
			//load up the activities
			Groups = new ObservableSortedUniqueCollection<string, ActivityGroupViewModel>(new ActivityGroupViewModel.ActivityAgeComparitor());
			Activities = SnooStreamViewModel.SystemServices.MakeIncrementalLoadCollection(new SelfActivityLoader(this), 100);
            if (IsLoggedIn)
			{
                ProcessActivityManager();
                RunActivityUpdater();
            }
		}

		public async void OnUserLoggedIn(UserLoggedInMessage obj)
		{
            SnooStreamViewModel.ActivityManager.OAuth = SnooStreamViewModel.RedditUserState != null && SnooStreamViewModel.RedditUserState.OAuth != null ?
                    JsonConvert.SerializeObject(SnooStreamViewModel.RedditUserState) : "";
			SnooStreamViewModel.ActivityManager.CanStore = SnooStreamViewModel.RedditUserState != null && SnooStreamViewModel.RedditUserState.IsDefault;
            RaisePropertyChanged("IsLoggedIn");
			RaisePropertyChanged("Activities");
			OldestMessage = null;
			OldestSentMessage = null;
			OldestActivity = null;
			Groups.Clear();
            if (IsLoggedIn)
            {
				await PullNew(false, true);
                if (!_runningActivityUpdater)
                {
                    RunActivityUpdater();
                }
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

		private string OldestMessage { get; set; }
		private string OldestSentMessage { get; set; }
		private string OldestActivity { get; set; }
		public DateTime? LastRefresh { get; set; }
		public ObservableSortedUniqueCollection<string, ActivityGroupViewModel> Groups { get; private set; }
		public ObservableCollection<ViewModelBase> Activities { get; private set; }
        public bool HasUnviewed { get; internal set; }

        public static Dictionary<string, ActivityViewModel> ActivityLookup = new Dictionary<string, ActivityViewModel>();
		public async Task PullNew(bool userInitiated, bool appStart)
        {
            LastRefresh = DateTime.Now;
            if (!IsLoggedIn)
                throw new InvalidOperationException("User must be logged in to do this");

			if (SnooStreamViewModel.ActivityManager.NeedsRefresh(appStart) || userInitiated)
                await SnooStreamViewModel.ActivityManager.Refresh();

            ProcessActivityManager();
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
			});
		}


        private async void ProcessActivityManager()
        {
            var resultTpl = await Task.Run(() =>
                {
                    Listing inbox = SnooStreamViewModel.ActivityManager.Received;
                    Listing outbox = SnooStreamViewModel.ActivityManager.Sent;
                    Listing activity = SnooStreamViewModel.ActivityManager.Activity;
                    return Tuple.Create(inbox, outbox, activity);
                });


            OldestMessage = ActivityGroupViewModel.ProcessListing(Groups, resultTpl.Item1, OldestMessage);
            OldestSentMessage = ActivityGroupViewModel.ProcessListing(Groups, resultTpl.Item2, OldestSentMessage);
            OldestActivity = ActivityGroupViewModel.ProcessListing(Groups, resultTpl.Item3, OldestActivity);

            HasUnviewed = Groups.Values.Any(group => group.HasUnviewed);
        }

        bool _runningActivityUpdater = false;
        public async void RunActivityUpdater()
        {
            _runningActivityUpdater = true;
            try
            {
                var cancelToken = SnooStreamViewModel.BackgroundCancellationToken;
                await PullNew(false, true);
                while (!cancelToken.IsCancellationRequested)
                {
                    //check every 5 minutes since that is the minimum time we might refresh at
                    if (SnooStreamViewModel.ActivityManager.NeedsRefresh(false))
                    {
                        await PullNew(false, true);
                    }
                    await Task.Delay(1000 * 60 * 5, cancelToken);
                }
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            _runningActivityUpdater = false;
        }

		public async Task PullOlder()
		{
			LastRefresh = DateTime.Now;

			if (!IsLoggedIn)
				throw new InvalidOperationException("User must be logged in to do this");

            OldestMessage = await PullActivity(OldestMessage, "inbox", string.Format(Reddit.MailBaseUrlFormat, "inbox"));
            OldestSentMessage = await PullActivity(OldestSentMessage, "outbox", string.Format(Reddit.MailBaseUrlFormat, "sent"));
            OldestActivity = await PullActivity(OldestActivity, "activity", string.Format(Reddit.PostByUserBaseFormat, SnooStreamViewModel.RedditService.CurrentUserName));
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

