using SnooSharp;
using SnooStream.Common;
using SnooStream.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class SubredditSidebarViewModel : SnooObservableObject, IHasTitle, IHasHubNavCommands, IRefreshable, IHasLoadableState
    {
        INavigationContext NavigationContext;
        ISubredditContext Context;
        public SubredditSidebarViewModel(INavigationContext navContext, ISubredditContext context)
        {
            NavigationContext = navContext;
            Context = context;
            LoadState = new LoadViewModel
            {
                LoadAction = Load,
                IsCritical = true
            };

            Commands = Context.MakeHubNavCommands(this);
            Recommendations = new RecommendationCollection() { Context = Context, NavigationContext = NavigationContext };
            Moderators = new ModeratorCollection() { Context = Context, NavigationContext = NavigationContext };
        }

        public LoadItemCollectionBase Recommendations { get; set; }
        public LoadItemCollectionBase Moderators { get; set; }
        public SimpleMarkdownContainer DescriptionMD { get; set; }
        public LoadViewModel LoadState { get; set; }
        public IEnumerable<IHubNavCommand> Commands { get; set; }
        public Subreddit Thing { get; set; }
        public string Title
        {
            get
            {
                return Context.SubredditName;
            }
        }

        public void Refresh()
        {
            throw new NotImplementedException();
        }

        private async Task Load(IProgress<float> arg1, CancellationToken arg2)
        {
            var thing = await Context.Load(arg1, arg2, false);
            Thing = thing.Data as Subreddit;
            DescriptionMD = new SimpleMarkdownContainer(((Subreddit)thing.Data).Description);
            RaisePropertyChanged(nameof(Thing));
            RaisePropertyChanged(nameof(DescriptionMD));
        }
    }

    public class SubredditRecommendationViewModel
    {
        public INavigationContext NavigationContext { get; set; }
        public Recommendation Thing { get; set; }
        public void Navigate()
        {
            Navigation.GotoSubreddit(Thing.Subreddit, NavigationContext);
        }
    }

    public class RecommendationCollection : LoadItemCollectionBase
    {
        public ISubredditContext Context { get; set; }
        public INavigationContext NavigationContext { get; set; }
        protected override Task LoadAdditional(IProgress<float> progress, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        protected override async Task LoadInitial(IProgress<float> progress, CancellationToken token)
        {
            var recommendations = await Context.LoadRecommendations(progress, token, false);
            AddRange(SubredditSidebarBuilder.MakeRecommendations(recommendations, NavigationContext));
        }

        protected override async Task Refresh(IProgress<float> progress, CancellationToken token)
        {
            var recommendations = await Context.RefreshRecommendations(progress, token);
            AddRange(SubredditSidebarBuilder.MakeRecommendations(recommendations, NavigationContext));
        }
    }

    public class ModeratorCollection : LoadItemCollectionBase
    {
        public ISubredditContext Context { get; set; }
        public INavigationContext NavigationContext { get; set; }
        public override bool HasMoreItems
        {
            get
            {
                return base.HasMoreItems && Context.HasAdditionalModerators;
            }
        }

        protected override bool IsMergable
        {
            get
            {
                return true;
            }
        }

        protected override async Task Refresh(IProgress<float> progress, CancellationToken token)
        {
            var moderators = await Context.RefreshModerators(progress, token);
            ClearItems();
            AddRange(SubredditSidebarBuilder.MakeModerators(moderators, NavigationContext));
        }

        protected override async Task LoadInitial(IProgress<float> progress, CancellationToken token)
        {
            var moderators = await Context.LoadModerators(progress, token, false);
            AddRange(SubredditSidebarBuilder.MakeModerators(moderators, NavigationContext));
        }

        protected override async Task LoadAdditional(IProgress<float> progress, CancellationToken token)
        {
            var moderators = await Context.LoadAdditionalModerators(progress, token);
            AddRange(SubredditSidebarBuilder.MakeModerators(moderators, NavigationContext));
        }
    }

    public interface ISubredditContext
    {
        void Subscribe();
        void Unsubscribe();
        string SubredditName { get; }

        Task<Thing> Load(IProgress<float> progress, CancellationToken token, bool ignoreCache);

        Task<Listing> LoadModerators(IProgress<float> progress, CancellationToken token, bool ignoreCache);
        Task<Listing> LoadAdditionalModerators(IProgress<float> progress, CancellationToken token);
        Task<Listing> RefreshModerators(IProgress<float> progress, CancellationToken token);
        bool HasAdditionalModerators { get; }


        Task<IEnumerable<Recommendation>> LoadRecommendations(IProgress<float> progress, CancellationToken token, bool ignoreCache);
        Task<IEnumerable<Recommendation>> RefreshRecommendations(IProgress<float> progress, CancellationToken token);
        IEnumerable<IHubNavCommand> MakeHubNavCommands(IRefreshable refreshTarget);
    }

    public class SubredditContext : ISubredditContext
    {
        public SnooSharp.Reddit Reddit { get; set; }
        private string _lastModerator;
        private bool _hasLoadedModerators = false;
        public bool HasAdditionalModerators
        {
            get
            {
                return !_hasLoadedModerators || !string.IsNullOrWhiteSpace(_lastModerator);
            }
        }

        public string SubredditName { get; set; }

        public async Task<Thing> Load(IProgress<float> progress, CancellationToken token, bool ignoreCache)
        {
            return await Reddit.GetSubredditAbout(SubredditName, token, progress);
        }

        public async Task<Listing> LoadAdditionalModerators(IProgress<float> progress, CancellationToken token)
        {
            var resultListing = await Reddit.GetAdditionalFromListing(Reddit.SubredditAboutBaseUrlFormat, _lastModerator, token, progress, false);
            if (resultListing != null)
                _lastModerator = resultListing.Data.After;
            return resultListing;
        }

        public async Task<Listing> LoadModerators(IProgress<float> progress, CancellationToken token, bool ignoreCache)
        {
            _hasLoadedModerators = true;
            var resultListing = await Reddit.GetSubredditAbout(SubredditName, "moderators", null, token, progress, false);
            if (resultListing != null)
                _lastModerator = resultListing.Data.After;
            return resultListing;
        }

        public async Task<IEnumerable<Recommendation>> LoadRecommendations(IProgress<float> progress, CancellationToken token, bool ignoreCache)
        {
            return await Reddit.GetRecomendedSubreddits(new string[] { SubredditName }, token, progress);
        }

        public IEnumerable<IHubNavCommand> MakeHubNavCommands(IRefreshable refreshTarget)
        {
            return Enumerable.Empty<IHubNavCommand>();
        }

        public Task<Listing> RefreshModerators(IProgress<float> progress, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Recommendation>> RefreshRecommendations(IProgress<float> progress, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public async void Subscribe()
        {
            await Reddit.AddSubredditSubscription(SubredditName, false);
        }

        public async void Unsubscribe()
        {
            await Reddit.AddSubredditSubscription(SubredditName, true);
        }
    }

    public static class SubredditSidebarBuilder
    {
        public static IEnumerable<SubredditRecommendationViewModel> MakeRecommendations(IEnumerable<Recommendation> recommendations, INavigationContext context)
        {
            return recommendations.Select(recommendation => new SubredditRecommendationViewModel { NavigationContext = context, Thing = recommendation }).ToList();
        }

        public static IEnumerable<UserViewModel> MakeModerators(Listing moderators, INavigationContext navContext)
        {
            List<UserViewModel> result = new List<UserViewModel>();
            foreach (var thing in moderators.Data.Children)
            {
                if (thing.Data is Account)
                {
                    result.Add(navContext.MakeUserDetailsContext(((Account)thing.Data).Name));
                }
            }
            return result;
        }
    }
}
