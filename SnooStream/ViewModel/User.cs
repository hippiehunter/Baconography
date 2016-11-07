using SnooSharp;
using SnooStream.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace SnooStream.ViewModel
{
    public interface IUserContext
    {
        string TargetUser { get; }
        bool HasAdditional { get; }
        Task<TypedThing<LabeledMulti>[]> LoadMultiReddits(IProgress<float> progress, CancellationToken token, bool ignoreCache);
        Task<Listing> Load(IProgress<float> progress, CancellationToken token, bool ignoreCache);
        Task<Thing> LoadUserInfo(IProgress<float> progress, CancellationToken token, bool ignoreCache);
        Task<Listing> LoadAdditional(IProgress<float> progress, CancellationToken token);
        void GotoComment(string url);
        void GotoMultiReddit(string url);
        void CopyMultiReddit(string from, string to, string displayName);
        //this is so we can take advantage of LinkBuilder in the search results
        ILinkBuilderContext LinkBuilderContext { get; }
        ICommentBuilderContext CommentBuilderContext { get; }

    }

    public class UserViewModel : IHasLoadableState
    {
        public IUserContext Context;
        public INavigationContext NavigationContext;
        public UserViewModel(IUserContext userContext, INavigationContext navigationContext)
        {
            NavigationContext = navigationContext;
            TargetUser = userContext.TargetUser;
            Context = userContext;
            KarmaCollection = new ObservableCollection<KarmaData>();
            Activity = new UserActivityCollection(userContext);
            MultiReddits = new UserMultiRedditCollection(userContext);
            LoadState = new LoadViewModel
            {
                LoadAction = async (progress, token) =>
                {
                    Thing = (await Context.LoadUserInfo(progress, token, false)).Data as Account;
                    KarmaCollection.Clear();
                    KarmaCollection.Add(new KarmaData { Name = "Comment", Value = Thing.CommentKarma });
                    KarmaCollection.Add(new KarmaData { Name = "Link", Value = Thing.LinkKarma });
                },
                IsCritical = true
            };
        }

        public string CakeDay
        {
            get
            {
                return (Thing != null ? Thing.CreatedUTC : new DateTime()).ToString("MMMM d");
            }
        }

        public void NavigateTo()
        {
            Navigation.GotoUserDetails(TargetUser, NavigationContext);
        }

        public string TargetUser { get; set; }
        public Account Thing { get; set; }
        public UserActivityCollection Activity { get; set; }
        public UserMultiRedditCollection MultiReddits { get; set; }
        public ObservableCollection<KarmaData> KarmaCollection { get; set; }
        public LoadViewModel LoadState { get; private set; }
    }

    public class KarmaData
    {
        public string Name
        {
            get;
            set;
        }

        public int Value
        {
            get;
            set;
        }
    }

    class UserContext : IUserContext
    {
        public UserContext(string username, Reddit reddit, INavigationContext navigationContext, OfflineService offline, ILinkContext linkContext)
        {
            _reddit = reddit;
            _navigationContext = navigationContext;
            TargetUser = username;
            CommentBuilderContext = new UserCommentBuilderContext { NavigationContext = navigationContext, Reddit = reddit,  };
            LinkBuilderContext = new LinkBuilderContext { LinkContext = linkContext, Offline = offline, Reddit = reddit, Subreddit = null, NavigationContext = navigationContext };
        }

        INavigationContext _navigationContext;
        Reddit _reddit;
        public ICommentBuilderContext CommentBuilderContext { get; set; }
        public ILinkBuilderContext LinkBuilderContext { get; set; }
        string _after;
        bool _hasLoaded = false;
        public bool HasAdditional
        {
            get
            {
                return !_hasLoaded || !string.IsNullOrWhiteSpace(_after);
            }
        }

        public string TargetUser { get; set; }

        public async void CopyMultiReddit(string from, string to, string displayName)
        {
            await _reddit.CopyMulti(from, from, displayName);
        }

        public void GotoComment(string url)
        {
            throw new NotImplementedException();
        }

        public void GotoMultiReddit(string url)
        {
            throw new NotImplementedException();
        }

        public async Task<Listing> Load(IProgress<float> progress, CancellationToken token, bool ignoreCache)
        {
            _hasLoaded = true;
            var listing = await _reddit.GetPostsByUser(TargetUser, null, token, progress, ignoreCache);
            _after = listing.Data.After;
            return listing;
        }

        public Task<TypedThing<LabeledMulti>[]> LoadMultiReddits(IProgress<float> progress, CancellationToken token, bool ignoreCache)
        {
            return _reddit.GetUserMultis(TargetUser, token, progress, ignoreCache);
        }

        public Task<Listing> LoadAdditional(IProgress<float> progress, CancellationToken token)
        {
            return _reddit.GetAdditionalFromListing(Reddit.PostByUserBaseFormat, _after, token, progress, true, null);
        }

        public async Task<Thing> LoadUserInfo(IProgress<float> progress, CancellationToken token, bool ignoreCache)
        {
            return await _reddit.GetAccountInfo(TargetUser, token, progress, ignoreCache);
        }
    }

    static class UserViewModelBuilder
    {
        public static IEnumerable<object> MakeMultiReddits(IEnumerable<Thing> things, IUserContext builderContext)
        {
            List<object> result = new List<object>();
            foreach (var thing in things)
            {
                if (thing.Data is LabeledMulti)
                {
                    result.Add(new UserMultiRedditViewModel { Context = builderContext, Thing = thing.Data as LabeledMulti, Name = ((LabeledMulti)thing.Data).Name });
                }
            }
            return result;
        }

        public static IEnumerable<object> MakeViewModels(IEnumerable<LabeledMulti> things, IUserContext builderContext)
        {
            List<object> result = new List<object>();
            foreach (var thing in things)
            {
                result.Add(new UserMultiRedditViewModel { Context = builderContext, Name = thing.Name, Thing = thing });
            }
            return result;
        }

        public static IEnumerable<object> MakeViewModels(IEnumerable<Thing> things, IUserContext builderContext)
        {
            List<object> result = new List<object>();
            foreach (var thing in things)
            {
                if (thing.Data is Link)
                {
                    result.Add(new LinkViewModel { Context = builderContext.LinkBuilderContext.LinkContext, Thing = thing.Data as Link, Votable = new VotableViewModel(thing.Data as Link, builderContext.LinkBuilderContext.UpdateVotable), CommentCount = ((Link)thing.Data).CommentCount, FromMultiReddit = builderContext.LinkBuilderContext.IsMultiReddit });
                }
                else if (thing.Data is Comment)
                {
                    var comment = thing.Data as Comment;
                    var commentViewModel = new CommentViewModel
                    {
                        Depth = 0,
                        Thing = comment,
                        Votable = new VotableViewModel(thing.Data as Comment, builderContext.CommentBuilderContext.ChangeVote),
                        Context = builderContext.CommentBuilderContext,
                        _collapsed = false,
                        Body = WebUtility.HtmlDecode(comment.Body),
                        AuthorFlair = builderContext.CommentBuilderContext.GetUsernameModifiers(comment.Author),
                        AuthorFlairText = WebUtility.HtmlDecode(comment.AuthorFlairText)
                    };
                    commentViewModel.BodyMDTask = builderContext.CommentBuilderContext.QueueMarkdown(commentViewModel);
                    result.Add(commentViewModel);
                }
            }
            return result;
        }
    }

    class UserCommentBuilderContext : BasicCommentBuilderContext
    {
        public UserActivityCollection ActivityCollection { get; set; }
        public Reddit Reddit { get; set; }
        public INavigationContext NavigationContext { get; set; }

        public override string CurrentUserName
        {
            get
            {
                return Reddit.CurrentUserName;
            }
        }

        public override async void ChangeVote(string id, int direction)
        {
            await Reddit.AddVote(id, direction);
        }

        public override void Report(string id)
        {
            Reddit.AddReportOnThing(id);
        }

        public override void Save(string id)
        {
            Reddit.AddSavedThing(id);
        }

        public override void Share(CommentViewModel comment)
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager,
                DataRequestedEventArgs>((sender, e) =>
                {
                    DataRequest request = e.Request;
                    request.Data.Properties.Title = "Comment from user " + comment.Thing.Author;
                    request.Data.Properties.Description = comment.Thing.Body;
                    request.Data.SetWebLink(new Uri(comment.Thing.Context));
                });

            Windows.ApplicationModel.DataTransfer.DataTransferManager.ShowShareUI();
        }

        public override void GotoUserDetails(string author)
        {
            if (string.Compare(author, CurrentUserName, true) != 0)
                Navigation.GotoUserDetails(author, NavigationContext);
        }

        public override async void Delete(CommentViewModel comment)
        {
            ActivityCollection.Remove(comment);
            try
            {
                await Reddit.DeleteLinkOrComment(comment.Thing.Id);
            }
            catch { }
        }

        public override Task<Link> GetLink(IProgress<float> progress, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<Thing>> GetMore(IEnumerable<string> ids, IProgress<float> progress, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<Thing>> LoadAll(bool ignoreCache, IProgress<float> progress, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<Thing>> LoadRequested(bool ignoreCache, IProgress<float> progress, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public override Task SubmitComment(CommentViewModel viewModel)
        {
            throw new NotImplementedException();
        }
    }


    public class UserMultiRedditCollection : LoadItemCollectionBase
    {
        public IUserContext Context { get; set; }
        bool _hasLoaded = false;
        public UserMultiRedditCollection(IUserContext context)
        {
            Context = context;
        }

        public override bool HasMoreItems
        {
            get
            {
                //we dont currently support getting more than the initial 100 multi reddits for a user
                return base.HasMoreItems && !HasLoaded;
            }
        }

        protected override async Task Refresh(IProgress<float> progress, CancellationToken token)
        {
            var loadedListing = await Context.LoadMultiReddits(progress, token, true);
            AddRange(UserViewModelBuilder.MakeViewModels(loadedListing, Context));
        }

        protected override async Task LoadInitial(IProgress<float> progress, CancellationToken token)
        {
            var loadedListing = await Context.LoadMultiReddits(progress, token, false);
            AddRange(UserViewModelBuilder.MakeViewModels(loadedListing, Context));
        }

        protected override Task LoadAdditional(IProgress<float> progress, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }


    public class UserActivityCollection : LoadItemCollectionBase
    {
        public IUserContext Context { get; set; }
        
        public UserActivityCollection(IUserContext context)
        {
            Context = context;
        }

        public override bool HasMoreItems
        {
            get
            {
                return base.HasMoreItems && Context.HasAdditional;
            }
        }
        
        protected override async Task Refresh(IProgress<float> progress, CancellationToken token)
        {
            var loadedListing = await Context.Load(progress, token, true);
            AddRange(UserViewModelBuilder.MakeViewModels(loadedListing.Data.Children, Context));
        }

        protected override async Task LoadInitial(IProgress<float> progress, CancellationToken token)
        {
            var loadedListing = await Context.Load(progress, token, false);
            AddRange(UserViewModelBuilder.MakeViewModels(loadedListing.Data.Children, Context));
        }

        protected override async Task LoadAdditional(IProgress<float> progress, CancellationToken token)
        {
            var loadedListing = await Context.LoadAdditional(progress, token);
            AddRange(UserViewModelBuilder.MakeViewModels(loadedListing.Data.Children, Context));
        }
    }

    public class UserMultiRedditViewModel
    {
        public IUserContext Context { get; set; }
        public LabeledMulti Thing { get; set; }
        public string Name { get; set; }

        public void Clone()
        {
            Context.CopyMultiReddit(string.Format("/user/{0}/m/{1}", Context.TargetUser, Thing.Path), "/me/m/" + Thing.Path, Thing.Name);
        }

        public void Navigate()
        {
            Context.GotoMultiReddit(Thing.Path);
        }
    }

    class UserLinkContext : BaseLinkContext
    {
        public UserViewModel UserViewModel { get; set; }
        public override void GotoLink(LinkViewModel vm)
        {
            Navigation.GotoLink(UserViewModel, vm.Thing.Url, NavigationContext);
        }
    }
}
