using GalaSoft.MvvmLight;
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
	public class SelfStreamViewModel : ViewModelBase
	{
		public class SelfActivityAggregate : IIncrementalCollectionLoader<ViewModelBase>
		{
			SelfStreamViewModel _selfStream;
			ObservableCollection<ViewModelBase> _targetCollection;
			public SelfActivityAggregate(SelfStreamViewModel selfStream)
			{
				_selfStream = selfStream;
				_selfStream.Groups.CollectionChanged += _groups_CollectionChanged;
			}

			void RegisterGroup(ActivityGroupViewModel group)
			{
				group.Activities.CollectionChanged += Activities_CollectionChanged;
				group.PropertyChanged += group_PropertyChanged;
			}

			void group_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
			{
				if (e.PropertyName == "IsExpanded")
				{
					var group = sender as ActivityGroupViewModel;
					if (group.IsExpanded)
					{
						//need to make sure no one else is marked as expanded
						//because we only allow one at a time
						var existingExpanded = _targetCollection.FirstOrDefault(vm => vm != group && vm is ActivityGroupViewModel && ((ActivityGroupViewModel)vm).IsExpanded) as ActivityGroupViewModel;
						if (existingExpanded != null)
							existingExpanded.IsExpanded = false;

						if (group.Activities.Count > 1)
						{
							var indexOfGroup = _targetCollection.IndexOf(group);
							foreach (var activity in group.Activities)
							{
								_targetCollection.Insert(++indexOfGroup, activity);
							}
						}
					}
					else
					{
						//remove all of the activities from this group
						if (group.Activities.Count > 1)
						{
							foreach (var item in group.Activities)
								_targetCollection.Remove(item);
						}
					}
				}
			}

			void Activities_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
			{

				switch (e.Action)
				{
					case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
						break;
					case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
						break;
					case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
						break;
					case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
						break;
					case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
						break;
					default:
						break;
				}
			}

			void UnregisterGroup(ActivityGroupViewModel group)
			{
				group.Activities.CollectionChanged -= Activities_CollectionChanged;
				group.PropertyChanged -= group_PropertyChanged;
			}

			void _groups_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
			{
				var collection = sender as ObservableSortedUniqueCollection<string, ActivityGroupViewModel>;
				switch (e.Action)
				{
					case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
						RegisterGroup(e.NewItems[0] as ActivityGroupViewModel);
						var followingGroup = collection.GetElementFollowing(e.NewItems[0] as ActivityGroupViewModel);
						if (followingGroup != null)
						{
							_targetCollection.Insert(Math.Max(0, _targetCollection.IndexOf(followingGroup) - 1), e.NewItems[0] as ActivityGroupViewModel);
						}
						else
						{
							_targetCollection.Add(e.NewItems[0] as ActivityGroupViewModel);
						}

						break;
					case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
						break;
					case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
						var removedGroup = e.OldItems[0] as ActivityGroupViewModel;
						UnregisterGroup(removedGroup);
						if (removedGroup.IsExpanded)
						{
							foreach (var item in removedGroup.Activities)
								_targetCollection.Remove(item);
						}
						break;
					case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
						break;
					case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
						_targetCollection.Clear();
						break;
					default:
						break;
				}
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
				await _selfStream.PullNew();
			}

			public string NameForStatus
			{
				get { return "activitie"; }
			}

			public void Attach(System.Collections.ObjectModel.ObservableCollection<ViewModelBase> targetCollection)
			{
				_targetCollection = targetCollection;
			}
		}

		public SelfStreamViewModel(SelfInit selfInit)
		{
			//load up the activities
			Groups = new ObservableSortedUniqueCollection<string, ActivityGroupViewModel>(new ActivityGroupViewModel.ActivityAgeComparitor());
			Activities = SnooStreamViewModel.SystemServices.MakeIncrementalLoadCollection(new SelfActivityAggregate(this), 100);
			if (selfInit != null)
			{
				if (selfInit.SelfThings != null)
				{
					foreach (var thing in selfInit.SelfThings)
					{
						var thingName = ActivityViewModel.GetActivityGroupName(thing);
						ActivityGroupViewModel existingGroup;
						if (Groups.TryGetValue(thingName, out existingGroup))
						{
							existingGroup.Merge(thing);
						}
						else
						{
							Groups.Add(thingName, ActivityGroupViewModel.MakeActivityGroup(thing));
						}
					}
				}

				LastRefresh = selfInit.LastRefresh;
				OldestMessage = selfInit.AfterSelfMessage;
				OldestSentMessage = selfInit.AfterSelfSentMessage;
				OldestActivity = selfInit.AfterSelfAction;
			}

			MessengerInstance.Register<UserLoggedInMessage>(this, OnUserLoggedIn);
		}

		private async void OnUserLoggedIn(UserLoggedInMessage obj)
		{
			RaisePropertyChanged("IsLoggedIn");
			RaisePropertyChanged("Activities");
			Groups.Clear();
			await PullNew();
		}

		public bool IsLoggedIn
		{
			get
			{
				return !String.IsNullOrWhiteSpace(SnooStreamViewModel.RedditService.CurrentUserName);
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
		public async Task PullNew()
		{
			LastRefresh = DateTime.Now;
			if (!IsLoggedIn)
				throw new InvalidOperationException("User must be logged in to do this");

			Listing inbox = null;
			Listing outbox = null;
			Listing activity = null;

			await SnooStreamViewModel.NotificationService.Report("refreshing inbox", async () =>
			{
				inbox = await SnooStreamViewModel.RedditService.GetMessages(null);
			});

			OldestMessage = ProcessListing(inbox, OldestMessage);

			await SnooStreamViewModel.NotificationService.Report("refreshing outbox", async () =>
			{
				outbox = await SnooStreamViewModel.RedditService.GetSentMessages(null);
			});

			OldestSentMessage = ProcessListing(outbox, OldestSentMessage);

			await SnooStreamViewModel.NotificationService.Report("refreshing activity", async () =>
			{
				activity = await SnooStreamViewModel.RedditService.GetPostsByUser(SnooStreamViewModel.RedditService.CurrentUserName, null);
			});

			OldestActivity = ProcessListing(activity, OldestActivity);
		}

		private string ProcessListing(Listing listing, string after)
		{
			if (listing != null)
			{
				foreach (var child in listing.Data.Children)
				{
					var childName = ActivityViewModel.GetActivityGroupName(child);
					ActivityGroupViewModel existingGroup;
					if (Groups.TryGetValue(childName, out existingGroup))
					{
						if (existingGroup.Activities.Count <= 1)
						{
							existingGroup.Merge(child);
						}
						else
							existingGroup.Merge(child);
					}
					else
					{
						Groups.Add(childName, ActivityGroupViewModel.MakeActivityGroup(child));
					}
				}

				if (string.IsNullOrWhiteSpace(after))
					return listing.Data.After;
			}
			return after;
		}

		public async Task PullOlder()
		{
			LastRefresh = DateTime.Now;

			if (!IsLoggedIn)
				throw new InvalidOperationException("User must be logged in to do this");

			Listing inbox = null;
			Listing outbox = null;
			Listing activity = null;

			if (!string.IsNullOrWhiteSpace(OldestMessage))
			{
				await SnooStreamViewModel.NotificationService.Report("getting additional inbox", async () =>
				{
					inbox = await SnooStreamViewModel.RedditService.GetAdditionalFromListing(string.Format(Reddit.MailBaseUrlFormat, "inbox"), OldestMessage, null);
				});

				OldestMessage = ProcessListing(inbox, OldestMessage);
			}

			if (!string.IsNullOrWhiteSpace(OldestSentMessage))
			{
				await SnooStreamViewModel.NotificationService.Report("getting additional outbox", async () =>
				{
					outbox = await SnooStreamViewModel.RedditService.GetAdditionalFromListing(string.Format(Reddit.MailBaseUrlFormat, "sent"), OldestSentMessage, null);
				});

				OldestSentMessage = ProcessListing(inbox, OldestSentMessage);
			}


			if (!string.IsNullOrWhiteSpace(OldestActivity))
			{
				await SnooStreamViewModel.NotificationService.Report("getting additional activity", async () =>
				{
					activity = await SnooStreamViewModel.RedditService.GetAdditionalFromListing(string.Format(Reddit.PostByUserBaseFormat, SnooStreamViewModel.RedditService.CurrentUserName), OldestActivity, null);
				});

				OldestActivity = ProcessListing(inbox, OldestActivity);
			}
		}

		internal SelfInit Dump()
		{
			return new SelfInit
			{
				LastRefresh = LastRefresh,
				SelfThings = new List<Thing>(DumpThings(Groups)),
				AfterSelfAction = OldestActivity,
				AfterSelfMessage = OldestMessage,
				AfterSelfSentMessage = OldestSentMessage
			};
		}
	}
}

