using GalaSoft.MvvmLight;
using SnooSharp;
using SnooStream.Common;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace SnooStream.ViewModel
{
    public interface ISubredditGroupBuilderContext
    {
        bool IsLoggedIn { get; }
        void GotoSubreddit(string url);
        Task<Listing> LoadSubreddits(IProgress<float> progress, CancellationToken token);
        Task<TypedThing<LabeledMulti>[]> LoadMultiReddits(IProgress<float> progress, CancellationToken token);
        Task<Subreddit> LoadFeaturedSubreddit(IProgress<float> progress, CancellationToken token);
        void ChangeSubscription(string url, bool subscribe);
        void ChangeMulti(string url, string multi, bool subscribe);
    }

    public class SubredditRiverViewModel : ObservableObject, IHasLoadableState
    {
        private ISubredditGroupBuilderContext subredditContext;

        public SubredditRiverViewModel(ISubredditGroupBuilderContext subredditContext)
        {
            this.subredditContext = subredditContext;
            LoadState = new LoadViewModel
            {
                LoadAction = async (progress, token) =>
                {
                    var fullListing = await SubredditGroupBuilder.MakeFullListing(subredditContext.LoadSubreddits(progress, token));
                    var multiReddits = await subredditContext.LoadMultiReddits(progress, token);
                    var featuredSubreddit = await subredditContext.LoadFeaturedSubreddit(progress, token);
                    var groups = SubredditGroupBuilder.MakeGroups(fullListing, multiReddits, featuredSubreddit, subredditContext);
                    SubredditGroups.Clear();
                    foreach (var group in groups)
                        SubredditGroups.Add(group);
                },
                IsCritical = true
            };

            CollectionView = new CollectionViewSource { Source = SubredditGroups, ItemsPath = new Windows.UI.Xaml.PropertyPath("Collection"), IsSourceGrouped = true };
        }

        public ObservableCollection<object> SubredditGroups { get; set; } = new ObservableCollection<object>();
        public CollectionViewSource CollectionView { get; set; }
        public DateTime? LastRefresh { get; private set; }
        public LoadViewModel LoadState { get; private set; }
    }

    public class SubredditGroupBuilder
    {
        public static void RemoveSubscribed(IList<SubredditGroup> existing, string subscribedUrl)
        {
            var subGroup = existing.FirstOrDefault(group => group.Name == "Subscribed");
            if (subGroup == null)
                throw new InvalidOperationException("missing subscriber group, cant remove subscription");

            foreach (var toBeRemoved in subGroup.Collection.Where(wrapper => string.Compare(wrapper.Thing.Url, subscribedUrl, true) == 0).ToArray())
            {
                subGroup.Collection.Remove(toBeRemoved);
            }
        }

        public static void RemoveMultiReddit(IList<SubredditGroup> existing, string multiRedditPath)
        {
            foreach (var toBeRemoved in existing.Where(group => group.IsMultiReddit && string.Compare(group.MultiReddit?.TypedData?.Path, multiRedditPath, true) == 0).ToArray())
            {
                existing.Remove(toBeRemoved);
            }
        }

        public static void RemoveFromMultiReddit(IList<SubredditGroup> existing, string multiRedditPath, string subredditUrl)
        {
            var subGroup = existing.FirstOrDefault(group => group.IsMultiReddit && string.Compare(group.MultiReddit?.TypedData?.Path, multiRedditPath, true) == 0);
            if (subGroup == null)
                throw new InvalidOperationException("missing multireddit group, cant remove from multi reddit");

            foreach (var toBeRemoved in subGroup.Collection.Where(wrapper => string.Compare(wrapper.Thing.Url, subredditUrl, true) == 0).ToArray())
            {
                subGroup.Collection.Remove(toBeRemoved);
            }
        }

        private static IEnumerable<Subreddit> MakeBuiltins(Subreddit featuredSubreddit)
        {
            var result = new List<Subreddit>
            {
                new Subreddit { Headertitle = "random", Url = "/r/random", Name = "Random", DisplayName="Random", Title="Random", Id="t5_fakeid" },
                new Subreddit { Headertitle = "front page", Url = "/", Name = "Front Page", DisplayName="Front Page", Title="Front Page", Id="t5_fakeid" },
                new Subreddit { Headertitle = "all", Url = "/r/all", Name = "all", DisplayName="all", Title="all", Id="t5_fakeid" },
            };
            if (featuredSubreddit != null)
                result.Add(featuredSubreddit);
            return result;
        }

        public static async Task<IEnumerable<TypedThing<Subreddit>>> MakeFullListing(Task<Listing> subreddits)
        {
            var listing = await subreddits;
            return listing.Data.Children.Where(thing => thing.Data is Subreddit).Select(thing => new TypedThing<Subreddit>(thing));
        }

        public static IEnumerable<SubredditGroup> MakeGroups(IEnumerable<TypedThing<Subreddit>> subscribed, IEnumerable<TypedThing<LabeledMulti>> multiReddits, Subreddit featuredSubreddit, ISubredditGroupBuilderContext context)
        {
            var result = new List<SubredditGroup>
            {
                new SubredditGroup { Collection = new ObservableCollection<SubredditWrapper>(MakeWrappers(MakeBuiltins(featuredSubreddit), context)), IsMultiReddit = false, Name = "RedditBuiltIn" },
                new SubredditGroup { Collection = new ObservableCollection<SubredditWrapper>(MakeWrappers(subscribed.Select(thing => thing.TypedData), context)), IsMultiReddit = false, Name = context.IsLoggedIn ? "Subscribed" : "Popular" }
            };
            result.AddRange(multiReddits.Select(reddit => new SubredditGroup { Collection = new ObservableCollection<SubredditWrapper>(MakeWrappers(reddit.TypedData.Subreddits.Select(sub => sub.Data), context)), IsMultiReddit = true, Name = reddit.TypedData.Name, MultiReddit = reddit }));
            return result;
        }

        public static void UpdateGroups(IList<SubredditGroup> existing, IEnumerable<TypedThing<Subreddit>> subscribed, IEnumerable<TypedThing<LabeledMulti>> multiReddits, Subreddit featuredSubreddit, ISubredditGroupBuilderContext context)
        {
            var newGroups = MakeGroups(subscribed, multiReddits, featuredSubreddit, context);
            var oldGroupLookup = existing.ToDictionary(group => group.Name);
            var matched = new List<SubredditGroup>();
            var toBeAdded = new List<SubredditGroup>();
            foreach (var newGroup in newGroups)
            {
                if (oldGroupLookup.ContainsKey(newGroup.Name))
                {
                    var oldGroup = oldGroupLookup[newGroup.Name];
                    matched.Add(oldGroup);
                    UpdateWrappers(oldGroup.Collection, newGroup.Collection, context);
                }
                else
                {
                    toBeAdded.Add(newGroup);
                }
            }


            //remove its from existing if its not matched
            //add to existing if its in toBeAdded
            foreach (var old in oldGroupLookup.Values)
            {
                if (!matched.Any(group => group.Name == old.Name))
                    existing.Remove(old);
            }

            foreach (var newWrapper in toBeAdded)
                existing.Add(newWrapper);
        }

        private static IEnumerable<SubredditWrapper> MakeWrappers(IEnumerable<Subreddit> things, ISubredditGroupBuilderContext context)
        {
            return things.Select(sub => new SubredditWrapper(sub.DisplayName, sub, context));
        }

        private static void UpdateWrappers(IList<SubredditWrapper> existingWrappers, IList<SubredditWrapper> newWrappers, ISubredditGroupBuilderContext context)
        {
            var oldGroupLookup = existingWrappers.ToDictionary(group => group.Name);
            var matched = new List<SubredditWrapper>();
            var toBeAdded = new List<SubredditWrapper>();
            foreach (var newWrapper in newWrappers)
            {
                if (oldGroupLookup.ContainsKey(newWrapper.Name))
                {
                    var oldGroup = oldGroupLookup[newWrapper.Name];
                    matched.Add(oldGroup);
                    oldGroup.Merge(newWrapper.Thing);
                }
                else
                {
                    toBeAdded.Add(newWrapper);
                }
            }

            //remove its from existingWrappers if its not matched
            //add to existingWrappers if its in toBeAdded
            foreach (var old in oldGroupLookup.Values)
            {
                if (!matched.Any(group => group.Name == old.Name))
                    existingWrappers.Remove(old);
            }

            foreach (var newWrapper in toBeAdded)
                existingWrappers.Add(newWrapper);
        }
    }

    public class SubredditGroup
    {
        public ISubredditGroupBuilderContext Context { get; set; }
        public bool IsBuiltIn { get; set; }
        public bool IsMultiReddit { get; set; }
        public TypedThing<LabeledMulti> MultiReddit { get; set; }
        public string Name { get; set; }
        public ObservableCollection<SubredditWrapper> Collection { get; set; }
        public override string ToString()
        {
            return "SubredditGroup: " + Name;
        }

        public void Navigate()
        {
            if (IsBuiltIn || !IsMultiReddit)
            {
                Context.GotoSubreddit("/");
            }
            else
            {
                Context.GotoSubreddit(MultiReddit.TypedData.Path);
            }
        }
    }
    public class SubredditWrapper : ObservableObject
    {
        public ISubredditGroupBuilderContext Context { get; set; }

        public SubredditWrapper(string name, Subreddit thing, ISubredditGroupBuilderContext context)
        {
            Context = context;
            Thing = thing;
            Name = name;
        }

        public void Merge(Subreddit thing)
        {
            Thing = thing;
            RaisePropertyChanged("Thing");
        }

        public string Name { get; set; }
        public Subreddit Thing { get; set; }
        public int HeaderImageWidth { get { return GetHeaderSizeOrDefault(true); } }
        public int HeaderImageHeight { get { return GetHeaderSizeOrDefault(false); } }

        private const int DefaultHeaderWidth = 125;
        private const int DefaultHeaderHeight = 50;

        private int GetHeaderSizeOrDefault(bool width)
        {
            if (Thing.HeaderSize == null || Thing.HeaderSize.Length < 2)
                return width ? DefaultHeaderWidth : DefaultHeaderHeight;
            else
                return width ? Thing.HeaderSize[0] : Thing.HeaderSize[1];
        }

        public void Navigate()
        {
            Context.GotoSubreddit(Thing.Url);
        }
    }

    class SubredditRiverContext : ISubredditGroupBuilderContext
    {
        public Reddit Reddit { get; set; }
        public INavigationContext NavigationContext { get; set; }
        public bool IsLoggedIn { get { return string.IsNullOrEmpty(Reddit.CurrentUserName); } }
        public async void ChangeMulti(string url, string multi, bool subscribe)
        {
            await Reddit.ChangeMulti(multi, url, subscribe);
        }

        public async void ChangeSubscription(string url, bool subscribe)
        {
            await Reddit.AddSubredditSubscription(url, !subscribe);
        }

        public void GotoSubreddit(string url)
        {
            Navigation.GotoSubreddit(url, NavigationContext);
        }

        public Task<Subreddit> LoadFeaturedSubreddit(IProgress<float> progress, CancellationToken token)
        {
            return Task.FromResult<Subreddit>(null);
        }

        public Task<TypedThing<LabeledMulti>[]> LoadMultiReddits(IProgress<float> progress, CancellationToken token)
        {
            if (string.IsNullOrEmpty(Reddit.CurrentUserName))
            {
                return Task.FromResult(new TypedThing<LabeledMulti>[0]);
            }
            else
            {
                return Reddit.GetUserMultis(token, progress);
            }
        }

        public Task<Listing> LoadSubreddits(IProgress<float> progress, CancellationToken token)
        {
            if (string.IsNullOrEmpty(Reddit.CurrentUserName))
            {
                return Reddit.GetSubreddits(null, token, progress, false);
            }
            else
            {
                return Reddit.GetSubscribedSubredditListing(token, progress, false);
            }
            
        }
    }
}

