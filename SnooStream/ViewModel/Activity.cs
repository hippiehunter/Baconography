using GalaSoft.MvvmLight;
using NBoilerpipePortable.Util;
using SnooSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Windows.Foundation;
using SnooStreamBackground;
using Newtonsoft.Json;
using SnooStream.Common;
using System.Net;

namespace SnooStream.ViewModel
{
    public class ActivitiesViewModel : SnooObservableObject
    {
        public LoadViewModel LoadState { get; set; }
        public IActivityBuilderContext Context { get; set; }
        public DateTime? LastRefresh { get; set; }
        public ActivityCollection Activities { get; set; }

        public ActivitiesViewModel()
        {
        }

        public ActivitiesViewModel(IActivityBuilderContext activityContext)
        {
            Context = activityContext;
            Activities = new ActivityCollection { Context = Context };
            LoadState = new LoadViewModel { IsCritical = false, State = ViewModel.LoadState.None };
        }

        public async Task LoadAsync(IProgress<float> progress, CancellationToken token, bool ignoreCache)
        {
            var activityListing = await Context.Load(progress, token, ignoreCache);
            LastRefresh = activityListing.DataAge ?? DateTime.UtcNow;
            ActivityBuilder.UpdateActivityGroups(Activities, activityListing.Data.Children, Context);
        }

        public async Task LoadAdditionalAsync(IProgress<float> progress, CancellationToken token)
        {
            var activityListing = await Context.LoadAdditional(progress, token);
            ActivityBuilder.UpdateActivityGroups(Activities, activityListing.Data.Children, Context);
        }

        public void Refresh()
        {
            LoadState = new LoadViewModel { LoadAction = (progress, token) => LoadAsync(progress, token, true), IsCritical = false };
            RaisePropertyChanged("LoadState");
        }
    }

    public class ActivityCollection : RangedCollectionBase
    {
        public IActivityBuilderContext Context { get; set; }
        public ActivitiesViewModel Activities { get; set; }
        public override bool HasMoreItems
        {
            get
            {
                return Context.HasAdditional;
            }
        }

        public override IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            //Load Additional
            if (Count > 0)
            {
                var loadItem = new LoadViewModel { LoadAction = (progress, token) => Activities.LoadAdditionalAsync(progress, token), IsCritical = false };
                Add(loadItem);
                return LoadItem(loadItem).AsAsyncOperation();
            }
            else //Load fresh
            {
                var loadItem = new LoadViewModel { LoadAction = (progress, token) => Activities.LoadAsync(progress, token, false), IsCritical = true };
                Add(loadItem);
                return LoadItem(loadItem).AsAsyncOperation();
            }
        }

        private async Task<LoadMoreItemsResult> LoadItem(LoadViewModel loadItem)
        {
            var itemCount = Count - 1;
            await loadItem.LoadAsync();
            //now that the load is finished the load item should be removed from the list
            Remove(loadItem);
            return new LoadMoreItemsResult { Count = (uint)(Count - itemCount) };
        }
    }

    public class ActivityGroup
    {
        public List<ActivityViewModel> Children { get; set; }
    }

    public class ActivityViewModel
    {
        public string GroupId { get; set; }
        public string Id { get; set; }
        public Thing Thing { get; set; }
        public DateTime CreatedUTC { get; set; }
        public bool IsSelf { get; set; }
        public string PreviewBody { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Symbol { get; set; }
    }

    public class ActivityHeaderViewModel : SnooObservableObject
    {
        public DateTime CreatedUTC { get; set; }
        public string GroupId { get; set; }
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public string Symbol { get; set; }
        public bool IsExpandable { get; set; }
        public int ReadCount { get; set; }
        public int UnreadCount { get; set; }
    }

    public interface IActivityBuilderContext
    {
        string CurrentUserName { get; }
        bool TryGetGroup(string name, out ActivityGroup activityGroup);
        void AddGroup(string name, ActivityGroup activityGroup);
        bool HasAdditional { get; }
        Task<Listing> Load(IProgress<float> progress, CancellationToken token, bool ignoreCache);
        Task<Listing> LoadAdditional(IProgress<float> progress, CancellationToken token);
    }

    public static class ActivityBuilder
    {
        private static string ReplyStatusIcon = "\uE172";
        private static string ReplyAllStatusIcon = "\uE165";
        private static string SoloPostedStatusIcon = "\uE11C";
        private static string SoloCommentStatusIcon = "\uE110";
        private static string SoloMessageStatusIcon = "\uE122";
        public static IEnumerable<object> CreateActivityGroups(IEnumerable<Thing> things, IActivityBuilderContext context)
        {
            var madeGroups = new List<ActivityGroup>();
            var comparer = new ActivityAgeComparitor();
            var activities = things.Select(thing => CreateActivity(thing, context));
            foreach (var activity in activities)
            {
                ActivityGroup activityGroup;
                if (context.TryGetGroup(activity.GroupId, out activityGroup))
                {
                    AddSorted(activityGroup.Children, activity, comparer);
                }
                else
                {
                    activityGroup = new ActivityGroup { Children = new List<ActivityViewModel> { activity } };
                    context.AddGroup(activity.GroupId, activityGroup);
                }

                AddSorted(madeGroups, activityGroup, comparer);
            }

            List<object> flatList = new List<object>();
            foreach (var activity in madeGroups)
            {
                var firstActivity = activity.Children.First();
                flatList.Add(new ActivityHeaderViewModel
                {
                    GroupId = firstActivity.GroupId,
                    IsExpandable = activity.Children.Count > 1,
                    Title = firstActivity.Title,
                    SubTitle = firstActivity.Author,
                    Symbol = firstActivity.Symbol,
                    CreatedUTC = firstActivity.CreatedUTC
                });
            }

            return flatList;
        }

        public static void UpdateActivityGroups(IList<object> targetCollection, IEnumerable<Thing> things, IActivityBuilderContext context)
        {
            var comparer = new ActivityAgeComparitor();
            var activities = things.Select(thing => CreateActivity(thing, context));
            foreach (var activity in activities)
            {
                ActivityGroup activityGroup;
                if (context.TryGetGroup(activity.GroupId, out activityGroup))
                {
                    if (!activityGroup.Children.Any(vm => vm.Id == activity.Id))
                    {
                        AddSorted(activityGroup.Children, activity, comparer);
                        var collectionHeader = targetCollection.OfType<ActivityHeaderViewModel>().FirstOrDefault(header => header.GroupId == activityGroup.Children.First().GroupId);
                        var collectionHeaderIndex = targetCollection.IndexOf(collectionHeader) + 1;
                        var followingHeaderItem = targetCollection.Skip(collectionHeaderIndex).FirstOrDefault();
                        if (followingHeaderItem is ActivityViewModel)
                            targetCollection.Insert(collectionHeaderIndex + activityGroup.Children.IndexOf(activity), activity);
                    }
                }
                else
                {
                    activityGroup = new ActivityGroup { Children = new List<ActivityViewModel> { activity } };
                    context.AddGroup(activity.GroupId, activityGroup);
                }
            }
        }

        public static void ExpandActivity(IList<object> targetCollection, string groupId, IActivityBuilderContext context)
        {
            ActivityGroup group;
            if (context.TryGetGroup(groupId, out group))
            {
                var targetHeader = targetCollection.OfType<ActivityHeaderViewModel>().FirstOrDefault(header => header.GroupId == groupId && header.IsExpandable);
                if (targetHeader != null)
                {
                    var headerIndex = targetCollection.IndexOf(targetHeader);
                    var followingHeader = targetCollection.Skip(headerIndex + 1).OfType<ActivityHeaderViewModel>().FirstOrDefault();
                    if (followingHeader != null)
                    {
                        var targetIndex = headerIndex + 1;
                        var comparer = new ActivityAgeComparitor();
                        foreach (var child in group.Children)
                        {
                            targetCollection.Insert(targetIndex++, child);
                        }
                    }
                    else
                    {
                        //we must be at the end of the collection just add the already sorted children into the list
                        foreach (var child in group.Children)
                        {
                            targetCollection.Add(child);
                        }
                    }
                }
            }
        }

        public static void CollapseActivity(IList<object> targetCollection, string groupId, IActivityBuilderContext context)
        {
            ActivityGroup group;
            if (context.TryGetGroup(groupId, out group))
            {
                foreach (var activity in group.Children)
                    targetCollection.Remove(activity);
            }
        }

        public static void AddSorted<T>(IList<T> collection, T item, IComparer<T> comparer, int? initialLower = null, int? initialUpper = null)
        {
            int lower = initialLower ?? 0;
            int upper = initialUpper ?? collection.Count - 1;
            if (collection.Count == 0)
            {
                collection.Add(item);
                return;
            }
            if (comparer.Compare(collection[upper], item) <= 0)
            {
                if (upper == collection.Count - 1)
                    collection.Add(item);
                else
                    collection.Insert(upper, item);
                return;
            }
            if (comparer.Compare(collection[lower], item) >= 0)
            {
                collection.Insert(lower, item);
                return;
            }
            int index = BinarySearch(collection, item, initialLower, initialUpper, comparer);
            if (index < 0)
                index = ~index;
            collection.Insert(index, item);
        }

        public static int BinarySearch<T>(this IList<T> list, T value, int? initialLower = null, int? initialUpper = null, IComparer<T> comparer = null)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            comparer = comparer ?? Comparer<T>.Default;

            int lower = initialLower ?? 0;
            int upper = initialUpper ?? list.Count - 1;

            while (lower <= upper)
            {
                int middle = lower + (upper - lower) / 2;
                int comparisonResult = comparer.Compare(value, list[middle]);
                if (comparisonResult == 0)
                    return middle;
                else if (comparisonResult < 0)
                    upper = middle - 1;
                else
                    lower = middle + 1;
            }

            return ~lower;
        }

        public static string GetActivityGroupName(Thing thing)
        {
            if (thing == null)
                throw new ArgumentNullException();

            if (thing.Data is Link)
                return ((Link)thing.Data).Name;
            else if (thing.Data is Comment)
            {
                if (((Comment)thing.Data).LinkId != null)
                    return ((Comment)thing.Data).LinkId;
                else if (((Comment)thing.Data).Context != null)
                {
                    // "/r/{subreddit}/comments/{linkname}/{linktitleish}/{thingname}?context=3"
                    var splitContext = ((Comment)thing.Data).Context.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    return "t3_" + splitContext[3];
                }
                else
                    return ((Comment)thing.Data).ParentId;
            }
            else if (thing.Data is Message)
            {
                var messageThing = thing.Data as Message;
                if (messageThing.WasComment)
                {
                    // "/r/{subreddit}/comments/{linkname}/{linktitleish}/{thingname}?context=3"

                    var splitContext = messageThing.Context.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    return "t3_" + splitContext[3];
                }
                else
                {
                    return string.IsNullOrWhiteSpace(messageThing.FirstMessageName) ? messageThing.Name : messageThing.FirstMessageName;
                }
            }
            else if (thing.Data is ModAction)
            {
                return ((ModAction)thing.Data).TargetFullname;
            }
            else
                throw new ArgumentOutOfRangeException();
        }

        public static string GetAuthor(ActivityViewModel viewModel)
        {
            if (viewModel.Thing.Data is Message)
                return ((Message)viewModel.Thing.Data).Author;
            else
                return null;
        }

        public static string MakeActivityIdentifier(Thing thing)
        {
            var ident = "";
            if (thing.Data is Message)
            {
                var message = thing.Data as Message;
                ident += message.Author;
                ident += message.Body.GetHashCode();
                ident += message.CreatedUTC;
            }
            else
                ident = ((ThingData)thing.Data).Name;
            return ident;
        }

        public static ActivityViewModel CreateActivity(Thing thing, IActivityBuilderContext context)
        {
            ActivityViewModel result;
            var activityIdentifier = ActivityBuilder.MakeActivityIdentifier(thing);

            if (thing.Data is Link)
            {
                var body = WebUtility.HtmlDecode(((Link)thing.Data).Selftext);
                result = new ActivityViewModel
                {
                    Thing = thing,
                    CreatedUTC = ((Link)thing.Data).CreatedUTC,
                    Id = activityIdentifier,
                    GroupId = GetActivityGroupName(thing),
                    IsSelf = string.Compare(((Link)thing.Data).Author, context.CurrentUserName, true) == 0,
                    Symbol = SoloPostedStatusIcon,
                    PreviewBody = body.Length > 100 ? body.Remove(100) : body,
                    Author = ((Link)thing.Data).Author,
                    Title = ((Link)thing.Data).Title
                };
            }
            else if (thing.Data is Comment)
            {
                var body = WebUtility.HtmlDecode(((Comment)thing.Data).Body);
                result = new ActivityViewModel
                {
                    Thing = thing,
                    CreatedUTC = ((Comment)thing.Data).CreatedUTC,
                    Id = activityIdentifier,
                    GroupId = GetActivityGroupName(thing),
                    IsSelf = string.Compare(((Comment)thing.Data).Author, context.CurrentUserName, true) == 0,
                    Symbol = SoloCommentStatusIcon,
                    PreviewBody = body.Length > 100 ? body.Remove(100) : body,
                    Author = ((Comment)thing.Data).Author,
                    Title = ((Comment)thing.Data).LinkTitle
                };
            }
            else if (thing.Data is Message)
            {
                var messageThing = thing.Data as Message;
                var author = string.IsNullOrEmpty(messageThing.Author) ? "[deleted]" : messageThing.Author;
                var symbol = SoloMessageStatusIcon;
                bool isMod = false;
                if (messageThing.WasComment)
                {
                    symbol = ReplyAllStatusIcon;
                }
                //check if its actually mod mail
                else if (messageThing.Author == "reddit")
                {
                    isMod = true;
                }

                var body = WebUtility.HtmlDecode(((Message)thing.Data).Body);
                result = new ActivityViewModel
                {
                    Thing = thing,
                    CreatedUTC = ((Message)thing.Data).CreatedUTC,
                    Id = activityIdentifier,
                    GroupId = GetActivityGroupName(thing),
                    IsSelf = string.Compare(((Message)thing.Data).Author, context.CurrentUserName, true) == 0,
                    Symbol = SoloCommentStatusIcon,
                    PreviewBody = body.Length > 100 ? body.Remove(100) : body,
                    Author = ((Message)thing.Data).Author,
                    Title = messageThing.WasComment ? ((Message)thing.Data).LinkTitle : ((Message)thing.Data).Subject
                };
            }
            else
                throw new ArgumentOutOfRangeException();

            return result;
        }

        private static Tuple<string, bool> CleanAuthor(string author, IActivityBuilderContext context)
        {
            if (string.Compare(author, context.CurrentUserName, StringComparison.CurrentCultureIgnoreCase) == 0)
                return Tuple.Create("Me", true);
            else
                return Tuple.Create(author, false);
        }

        private static string StripCommonPrefix(string subject)
        {
            if (subject.ToLower().StartsWith("re:"))
                return subject.Substring(3).Trim();
            else
                return subject.Trim();
        }
    }

    public class ActivityAgeComparitor : IComparer<ActivityViewModel>, IComparer<ActivityGroup>, IComparer<object>
    {
        public int Compare(ActivityViewModel x, ActivityViewModel y)
        {
            //invert sort
            return GetCreated(y).CompareTo(GetCreated(x));
        }

        public int Compare(ActivityGroup x, ActivityGroup y)
        {
            //invert the sort
            var result = y.Children.FirstOrDefault().CreatedUTC.CompareTo(x.Children.FirstOrDefault().CreatedUTC);
            if (result == 0 && ((ThingData)y.Children.FirstOrDefault().Thing.Data).Id != ((ThingData)x.Children.FirstOrDefault().Thing.Data).Id)
                return 1;
            else
                return result;
        }

        private DateTime GetCreated(object x)
        {
            if (x is ActivityViewModel)
            {
                return ((ActivityViewModel)x).CreatedUTC;
            }
            else if (x is ActivityHeaderViewModel)
            {
                return ((ActivityHeaderViewModel)x).CreatedUTC;
            }
            else
                return new DateTime(); //sort unknowns to the bottom
        }

        public int Compare(object x, object y)
        {
            //invert sort
            return GetCreated(y).CompareTo(GetCreated(x));
        }
    }


    class ActivityBuilderContext : IActivityBuilderContext
    {
        public Reddit Reddit { get; set; }
        public ActivityManager ActivityManager { get; set; }
        private Dictionary<string, ActivityGroup> _groupLookup = new Dictionary<string, ActivityGroup>();
        private string _afterSent;
        private string _afterReceived;
        private string _afterActivity;
        private bool _hasLoaded;
        public string CurrentUserName
        {
            get
            {
                return Reddit.CurrentUserName;
            }
        }

        public bool HasAdditional
        {
            get
            {
                return !_hasLoaded ||
                    !string.IsNullOrWhiteSpace(_afterSent) ||
                    !string.IsNullOrWhiteSpace(_afterReceived) ||
                    !string.IsNullOrWhiteSpace(_afterActivity);
            }
        }

        public void AddGroup(string name, ActivityGroup activityGroup)
        {
            _groupLookup.Add(name, activityGroup);
        }

        public async Task<Listing> Load(IProgress<float> progress, CancellationToken token, bool ignoreCache)
        {
            if (ActivityManager.NeedsRefresh)
            {
                await ActivityManager.Refresh(JsonConvert.SerializeObject(Reddit.CurrentOAuth), (sender, args) => { }, true).AsTask(token, progress);
            }

            var sentListing = JsonConvert.DeserializeObject<Listing>(ActivityManager.SentBlob);
            var receivedListing = JsonConvert.DeserializeObject<Listing>(ActivityManager.ReceivedBlob);
            var activityListing = JsonConvert.DeserializeObject<Listing>(ActivityManager.ActivityBlob);
            _hasLoaded = true;

            return ProcessListings(sentListing, receivedListing, activityListing);
        }

        private Listing ProcessListings(Listing sentListing, Listing receivedListing, Listing activityListing)
        {
            _afterSent = sentListing?.Data?.After;
            _afterReceived = receivedListing?.Data?.After;
            _afterActivity = activityListing?.Data?.After;
            var resultListing = new Listing { Data = new ListingData { Children = new List<Thing>() } };
            if (sentListing?.Data?.Children != null)
                resultListing.Data.Children.AddRange(sentListing.Data.Children);

            if (receivedListing?.Data?.Children != null)
                resultListing.Data.Children.AddRange(receivedListing.Data.Children);

            if (activityListing?.Data?.Children != null)
                resultListing.Data.Children.AddRange(activityListing.Data.Children);

            if (resultListing.Data.Children.Count == 0)
                throw new RedditEmptyException("activity");

            return resultListing;
        }

        public async Task<Listing> LoadAdditional(IProgress<float> progress, CancellationToken token)
        {
            var sentListing = _afterSent != null ? await Reddit.GetAdditionalFromListing(string.Format(SnooSharp.Reddit.MailBaseUrlFormat, "sent"), _afterSent, token, progress, true, null) : null;
            var receivedListing = _afterReceived != null ? await Reddit.GetAdditionalFromListing(string.Format(SnooSharp.Reddit.MailBaseUrlFormat, "inbox"), _afterReceived, token, progress, true, null) : null;
            var activityListing = _afterActivity != null ? await Reddit.GetAdditionalFromListing(string.Format(SnooSharp.Reddit.PostByUserBaseFormat, CurrentUserName), _afterActivity, token, progress, true, null) : null;
            return ProcessListings(sentListing, receivedListing, activityListing);
        }

        public bool TryGetGroup(string name, out ActivityGroup activityGroup)
        {
            return _groupLookup.TryGetValue(name, out activityGroup);
        }
    }
}
