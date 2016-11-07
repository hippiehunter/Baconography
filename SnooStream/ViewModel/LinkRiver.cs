using GalaSoft.MvvmLight;
using SnooSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Windows.Foundation;
using System.Threading;
using SnooStream.Common;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using SnooStream.Controls;
using System.Net;

namespace SnooStream.ViewModel
{
    public class LinkRiverViewModel : SnooObservableObject, IHasTitle, IHasHubNavCommands, IRefreshable
    {
        public string Title { get { return Context.Title; } }
        public ILinkBuilderContext Context { get; set; }
        public string Sort { get; set; }
        public DateTime? LastRefresh { get; set; }
        public LoadItemCollectionBase Links {get; set;}
        public string LastLinkId { get; set; }
        public IEnumerable<IHubNavCommand> Commands { get; set; }

        public LinkRiverViewModel()
        {

        }

        public LinkRiverViewModel(ILinkBuilderContext context)
        {
            Context = context;
            Links = new LinkCollection { Context = context, LinkRiver = this };
            Commands = context.MakeHubNavCommands(this);
        }

        public bool IsUserMultiReddit
        {
            get
            {
                return Context.IsMultiReddit;
            }
        }

        private bool _isModerator;
        public bool IsModerator
        {
            get
            {
                var task = Context.IsModeratorReddit();
                if (task.IsCompleted)
                    return _isModerator = task.Result;
                else
                    task.ContinueWith(async tsk => Set(nameof(IsModerator), ref _isModerator, (await tsk)));

                return _isModerator;
            }
        }

        public bool IsMultiReddit
        {
            get
            {
                return Context.IsMultiReddit;
            }
        }

        public async Task<IEnumerable<LinkViewModel>> LoadAsync(IProgress<float> progress, CancellationToken token, bool ignoreCache)
        {
            var activityListing = await Context.Load(progress, token, ignoreCache);
            LastRefresh = activityListing.DataAge ?? DateTime.UtcNow;
            RaisePropertyChanged("Title");
            return LinkBuilder.AppendLinkViewModels(Links, activityListing.Data.Children, Context);
        }

        public async Task<IEnumerable<LinkViewModel>> LoadAdditionalAsync(IProgress<float> progress, CancellationToken token)
        {
            var activityListing = await Context.LoadAdditional(progress, token);
            return LinkBuilder.AppendLinkViewModels(Links, activityListing.Data.Children, Context);
        }

        public async Task<IEnumerable<LinkViewModel>> LoadCleanAsync(IProgress<float> progress, CancellationToken token)
        {
            var activityListing = await Context.Refresh(progress, token);
            var madeLinkViewModels = LinkBuilder.MakeLinkViewModels(activityListing.Data.Children, Context).ToList();
            CollectionMerger.Merge(Links, madeLinkViewModels);
            return Links.OfType<LinkViewModel>();
        }

        public async void Refresh()
        {
            await Links.Refresh();
        }
    }

    public class LinkCollection : LoadItemCollectionBase
    {
        public ILinkBuilderContext Context { get; set; }
        public LinkRiverViewModel LinkRiver { get; set; }
        public override bool HasMoreItems
        {
            get
            {
                return base.HasMoreItems && Context.HasAdditional;
            }
        }

        protected override bool IsMergable
        {
            get
            {
                return true;
            }
        }

        protected override Task Refresh(IProgress<float> progress, CancellationToken token)
        {
            return LinkRiver.LoadCleanAsync(progress, token);
        }

        protected override Task LoadInitial(IProgress<float> progress, CancellationToken token)
        {
            return LinkRiver.LoadAsync(progress, token, false);
        }

        protected override Task LoadAdditional(IProgress<float> progress, CancellationToken token)
        {
            return LinkRiver.LoadAdditionalAsync(progress, token);
        }
    }

    public interface ILinkContext
    {
        bool IsHighBandwidth { get; }
        string CurrentUser { get; }
        void GotoComments(LinkViewModel linkViewModel);
        Task<Thing> GetThing(string url, CancellationToken token, IProgress<float> progress);
        void UpdateVotable(string name, int direction);
        void GotoLink(LinkViewModel linkViewModel);
        void Share(LinkViewModel linkViewModel);
        void Hide(LinkViewModel linkViewModel);
        void Report(string id);
        void Save(string id);
        void Delete(LinkViewModel linkViewModel);
        void SubmitEdit(MarkdownEditingViewModel editing);
        void GotoUserDetails(LinkViewModel linkViewModel);
    }

    public interface ILinkBuilderContext
    {
        string Title { get; }
        void UpdateVotable(string name, int direction);
        Task<Dictionary<string, LinkMeta>> GenerateLinkMeta(IEnumerable<string> linkNames);
        void HandleError(Exception ex);
        bool HasAdditional { get; }
        Task<Listing> Load(IProgress<float> progress, CancellationToken token, bool ignoreCache);
        Task<Listing> LoadAdditional(IProgress<float> progress, CancellationToken token);
        Task<Listing> Refresh(IProgress<float> progress, CancellationToken token);
        bool IsMultiReddit { get; }
        bool IsUserMultiReddit { get; }
        Task<bool> IsModeratorReddit();
        ILinkContext LinkContext { get; }
        IEnumerable<IHubNavCommand> MakeHubNavCommands(IRefreshable refreshTarget);
    }

    public static class LinkBuilder
    {
        public static async Task<LinkViewModel> MakeLinkViewModel(string url, ILinkContext context, CancellationToken token, IProgress<float> progress)
        {
            var madeThing = await context.GetThing(url, token, progress);
            var linkViewModel = new LinkViewModel { Context = context, Thing = madeThing.Data as Link, Votable = new VotableViewModel(madeThing.Data as Link, context.UpdateVotable), CommentCount = ((Link)madeThing.Data).CommentCount, FromMultiReddit = false };
            return linkViewModel;
        }

        public static IEnumerable<LinkViewModel> MakeLinkViewModels(IEnumerable<Thing> things, ILinkBuilderContext builderContext)
        {
            var madeViewModels = things
                .Where(thing => thing.Data is Link)
                .Select(thing => new LinkViewModel { Context = builderContext.LinkContext, Thing = thing.Data as Link, Votable = new VotableViewModel(thing.Data as Link, builderContext.UpdateVotable), CommentCount = ((Link)thing.Data).CommentCount, FromMultiReddit = builderContext.IsMultiReddit }).ToList();
            UpdateLinkMetadata(things, builderContext, madeViewModels);
            return madeViewModels;
        }

        private static void UpdateLinkMetadata(IEnumerable<Thing> things, ILinkBuilderContext builderContext, IEnumerable<LinkViewModel> madeViewModels)
        {
            var metadataTask = builderContext.GenerateLinkMeta(things.Where(thing => thing.Data is Link).Select(thing => ((Link)thing.Data).Name));
            Action<Task<Dictionary<string, LinkMeta>>> attachMeta = tsk =>
            {
                //might need to build better infrestructure here around catching exceptions and reporting them in the correct context
                if (tsk.Exception != null)
                    builderContext.HandleError(tsk.Exception);
                else
                {
                    var metaLookup = tsk.Result;
                    foreach (var madeViewModel in madeViewModels)
                    {
                        if (metaLookup.ContainsKey(madeViewModel.Thing.Name))
                            madeViewModel.UpdateMetadata(metaLookup[madeViewModel.Thing.Name]);
                    }
                }
            };

            if (metadataTask.IsCompleted)
                attachMeta(metadataTask);
            else
                metadataTask.ContinueWith(attachMeta); 
        }

        public static void UpdateLinkViewModels(IList<LinkViewModel> existing, IEnumerable<Thing> things, ILinkBuilderContext builderContext)
        {
            var possibleReplaceCount = things.Count();
            var replacementZone = existing.Take(possibleReplaceCount);
            //remove all excess items since this is an update and replace operation
            for (int i = possibleReplaceCount; i < existing.Count; i++)
                existing.RemoveAt(i);

            var madeViewModels = things
                .Where(thing => thing.Data is Link)
                .Select(thing => new LinkViewModel { Thing = thing.Data as Link, Votable = new VotableViewModel(thing.Data as Link, builderContext.UpdateVotable), CommentCount = ((Link)thing.Data).CommentCount }).ToList();

            var oldLinkLookup = existing.ToDictionary(link => link.Thing.Name);
            var matched = new List<LinkViewModel>();
            var toBeAdded = new List<LinkViewModel>();
            foreach (var newLink in madeViewModels)
            {
                if (oldLinkLookup.ContainsKey(newLink.Thing.Name))
                {
                    var oldLink = oldLinkLookup[newLink.Thing.Name];
                    matched.Add(oldLink);
                    oldLink.UpdateThing(newLink.Thing);
                }
                else
                {
                    toBeAdded.Add(newLink);
                }
            }

            //remove its from existing if its not matched
            //add to existing if its in toBeAdded
            foreach (var old in oldLinkLookup.Values)
            {
                if (!matched.Any(link => link.Thing.Name == old.Thing.Name))
                    existing.Remove(old);
            }

            foreach (var newLink in toBeAdded)
                existing.Add(newLink);

            UpdateLinkMetadata(things, builderContext, existing);
        }

        public static IEnumerable<LinkViewModel> AppendLinkViewModels(IList<object> existing, IEnumerable<Thing> things, ILinkBuilderContext builderContext)
        {
            var addedViewModels = new List<LinkViewModel>();
            var madeViewModels = MakeLinkViewModels(things, builderContext);
            var oldLinkLookup = existing.OfType<LinkViewModel>().ToDictionary(link => link.Thing.Name);
            foreach(var madeViewModel in madeViewModels)
            {
                if (oldLinkLookup.ContainsKey(madeViewModel.Thing.Name))
                {
                    oldLinkLookup[madeViewModel.Thing.Name].UpdateThing(madeViewModel.Thing);
                }
                else
                {
                    existing.Add(madeViewModel);
                    addedViewModels.Add(madeViewModel);
                }
            }
            return addedViewModels;
        }
    }

    public class LinkViewModel : SnooObservableObject, IMergableViewModel<LinkViewModel>
    {
        public ILinkContext Context { get; set; }
        public Link Thing { get; set; }
        private object _selfText;
        public object SelfText
        {
            get
            {
                return _selfText;
            }
            set
            {
                Set("SelfText", ref _selfText, value);
            }
        }
        public string LinkTitle { get { return WebUtility.HtmlDecode(Thing.Title); } }
        public VotableViewModel Votable { get; set; }
        public LinkMeta Metadata { get; set; }
        public MarkdownEditingViewModel Editing { get; set; }
        public int CommentCount { get; set; }
        public int ReadCommentCount { get; set; }
        public bool FromMultiReddit { get; set; }
        public bool IsEditing { get; set; }
        public bool CanEdit { get { return string.Compare(Thing.Author, Context.CurrentUser, true) == 0; } }
        public bool CanDelete { get { return string.Compare(Thing.Author, Context.CurrentUser, true) == 0; } }
        public void UpdateThing(Link thing)
        {
            if (thing.CommentCount != CommentCount)
            {
                CommentCount = thing.CommentCount;
                RaisePropertyChanged("CommentCount");
            }

            if (Thing == null)
            {
                Thing = thing;
                RaisePropertyChanged("LinkTitle");
                RaisePropertyChanged("Thing");
            }
            else
            {
                Thing = thing;
            }
            
            Votable.MergeVotable(thing);
            
        }

        private PreviewResolution Preview
        {
            get
            {
                if (PreviewFull != null)
                {
                    var result = PreviewFull.resolutions.OrderByDescending(thing => thing.height * thing.width);
                    if (Context.IsHighBandwidth)
                        return result.FirstOrDefault();
                    else
                        return result.LastOrDefault();
                }
                else
                    return null;
            }
        }

        private PreviewImage PreviewFull
        {
            get
            {
                if (Thing.Preview?.images != null)
                {
                    return Thing.Preview.images.FirstOrDefault();
                }
                else
                    return null;
            }
        }

        public bool HasPreview { get { return Preview != null; } }

        public string PreviewSource
        {
            get
            {
                //this came through json so the string is still html encoded
                return WebUtility.HtmlDecode(Preview?.url);
            }
        }

        public double PreviewHeight
        {
            get
            {
                return Preview?.height ?? 0.0;
            }
        }

        public string MergeID
        {
            get
            {
                return Thing.Name;
            }
        }

        public void UpdateMetadata(LinkMeta linkMeta)
        {
            Metadata = linkMeta;
            if (linkMeta.CommentCount != ReadCommentCount)
            {
                ReadCommentCount = linkMeta.CommentCount;
                RaisePropertyChanged("ReadCommentCount");
            }
        }

        public void GotoLink()
        {
            Context.GotoLink(this);
        }

        public void GotoComments()
        {
            Context.GotoComments(this);
        }

        public void Share()
        {
            Context.Share(this);
        }

        public void Hide()
        {
            Context.Hide(this);
        }

        public void Report()
        {
            Context.Report(Thing.Id);
        }

        public void Save()
        {
            Context.Save(Thing.Id);
        }

        public void Edit()
        {
            
        }

        public void SubmitEdit()
        {
            IsEditing = false;
            RaisePropertyChanged("IsEditing");
            Context.SubmitEdit(Editing);
        }

        public void CancelEdit()
        {
            Editing = null;
            IsEditing = false;
            RaisePropertyChanged("Editing");
            RaisePropertyChanged("IsEditing");
        }

        public void Delete()
        {
            Context.Delete(this);
        }

        public void GotoUserDetails()
        {
            Context.GotoUserDetails(this);
        }

        public void Merge(LinkViewModel source)
        {
            UpdateThing(source.Thing);
        }
    }

    abstract class BaseLinkContext : ILinkContext
    {
        public INavigationContext NavigationContext { get; set; }
        public Reddit Reddit { get; set; }
        public string CurrentUser { get { return Reddit.CurrentUserName; } }

        public bool IsHighBandwidth
        {
            get
            {
                return true;
            }
        }

        public virtual void GotoComments(LinkViewModel link)
        {
            Navigation.GotoComments(link.Thing.Permalink, NavigationContext, link);
        }

        public Task<Thing> GetThing(string url, CancellationToken token, IProgress<float> progress)
        {
            return Reddit.GetLinkByUrl(url, token, progress, false);
        }

        public async void UpdateVotable(string name, int direction)
        {
            try
            {
                await Reddit.AddVote(name, direction);
            }
            catch { }
        }

        public abstract void GotoLink(LinkViewModel vm);

        public virtual void Share(LinkViewModel linkViewModel)
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager,
                DataRequestedEventArgs>((sender, e) =>
                {
                    DataRequest request = e.Request;
                    request.Data.Properties.Title = linkViewModel.Thing.Title;
                    request.Data.Properties.Description = linkViewModel.Thing.Permalink;
                    request.Data.SetWebLink(new Uri(linkViewModel.Thing.Url));
                });

            Windows.ApplicationModel.DataTransfer.DataTransferManager.ShowShareUI();
        }

        public virtual void Hide(LinkViewModel linkViewModel)
        {
            Reddit.HideThing(linkViewModel.Thing.Id);
        }

        public virtual void Report(string id)
        {
            Reddit.AddReportOnThing(id);
        }

        public virtual void Save(string id)
        {
            Reddit.AddSavedThing(id);
        }

        public async virtual void Delete(LinkViewModel linkViewModel)
        {
            try
            {
                await Reddit.DeleteLinkOrComment(linkViewModel.Thing.Id);
            }
            catch { }
        }

        public async virtual void SubmitEdit(MarkdownEditingViewModel editing)
        {
            try
            {
                await Reddit.EditPost(editing.Text, editing.Context.TargetName);
            }
            catch { }
        }

        public virtual void GotoUserDetails(LinkViewModel vm)
        {
            Navigation.GotoUserDetails(vm.Thing.Author, NavigationContext);
        }
    }

    class LinkContext : BaseLinkContext
    {
        public LinkRiverViewModel LinkRiver { get; set; }

        public override void GotoComments(LinkViewModel link)
        {
            LinkRiver.Links.CurrentItem = link;
            base.GotoComments(link);
        }

        public override void GotoLink(LinkViewModel vm)
        {
            LinkRiver.Links.CurrentItem = vm;
            Navigation.GotoLink(LinkRiver, vm.Thing.Url, NavigationContext);
        }

        public override void GotoUserDetails(LinkViewModel vm)
        {
            LinkRiver.Links.CurrentItem = vm;
            base.GotoUserDetails(vm);
        }

        public override void Hide(LinkViewModel linkViewModel)
        {
            LinkRiver.Links.Remove(linkViewModel);
            base.Hide(linkViewModel);
        }

        public override void Delete(LinkViewModel linkViewModel)
        {
            LinkRiver.Links.Remove(linkViewModel);
            base.Delete(linkViewModel);
        }
    }

    class LinkBuilderContext : ILinkBuilderContext
    {
        public INavigationContext NavigationContext { get; set; }
        public string Title { get { return MakeDisplaySubredditName(Subreddit); } }
        public ILinkContext LinkContext { get; set; }
        public string Subreddit { get; set; }
        public Lazy<Task<Subreddit>> Thing { get; set; }
        public string Sort { get; set; }
        public SnooSharp.Reddit Reddit { get; set; }
        public OfflineService Offline { get; set; }
        private string _after;
        private bool _hasLoaded = false;

        public LinkBuilderContext()
        {
            Thing = new Lazy<Task<SnooSharp.Subreddit>>(async () => (await Reddit.GetSubredditAbout(Subreddit, CancellationToken.None, new Progress<float>()))?.Data as Subreddit);
        }

        public IEnumerable<IHubNavCommand> MakeHubNavCommands(IRefreshable refreshTarget)
        {
            var result = new List<IHubNavCommand>
            {
                new PostNavCommand { NavigationContext = NavigationContext },
                new RefreshNavCommand { NavigationContext = NavigationContext, Target = refreshTarget },
                new SearchNavCommand { NavigationContext = NavigationContext, TargetSubreddit = Subreddit }
            };

            if (!IsMultiReddit)
                result.Add(new AboutSubredditNavCommand { NavigationContext = NavigationContext, TargetSubreddit = Subreddit });

            return result;
        }

        public static string MakeDisplaySubredditName(string subreddit)
        {
            if (string.IsNullOrWhiteSpace(subreddit) || subreddit == "/")
                return "front page";
            else
                return Reddit.MakePlainSubredditName(subreddit);
        }

        public bool IsMultiReddit
        {
            get
            {
                return string.IsNullOrWhiteSpace(Subreddit) || Subreddit.Contains("/m/") || Subreddit.Contains("+") || Subreddit == "/" || Subreddit.ToLower() == "/r/all";
            }
        }

        public bool IsUserMultiReddit
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Subreddit) && Subreddit.Contains("/m/");
            }
        }

        public async Task<bool> IsModeratorReddit()
        {
            return (await Thing.Value)?.Moderator ?? false;
        }

        public bool HasAdditional
        {
            get
            {
                return !_hasLoaded || !string.IsNullOrWhiteSpace(_after);
            }
        }

        public Task<Dictionary<string, LinkMeta>> GenerateLinkMeta(IEnumerable<string> linkNames)
        {
            return Task.Run(async () =>
            {
                return (await Offline.GetLinkMetadata(linkNames)).Where(lm => lm != null).ToDictionary(lm => ((ThingData)lm.Data).Name);
            });
        }

        public void HandleError(Exception ex)
        {
            //TODO proper error noticiation here
            Debug.WriteLine(ex);
        }

        public Task<Listing> Refresh(IProgress<float> progress, CancellationToken token)
        {
            _after = null;
            return Load(progress, token, true);
        }

        public async Task<Listing> Load(IProgress<float> progress, CancellationToken token, bool ignoreCache)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(Subreddit), "subreddit was null while loading links from LinkBuilderContext");
            _hasLoaded = true;
            var listing = await Reddit.GetPostsBySubreddit(Subreddit, token, progress, ignoreCache, Sort, null);
            if (!string.IsNullOrWhiteSpace(listing.RedirectedUrl))
            {
                Subreddit = "/r/" + Reddit.MakePlainSubredditName(listing.RedirectedUrl);
            }
            _after = listing.Data.After;
            return listing;
        }

        public async Task<Listing> LoadAdditional(IProgress<float> progress, CancellationToken token)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(Subreddit), "subreddit was null while loading additional links from LinkBuilderContext");
            var listing = await Reddit.GetAdditionalFromListing(Subreddit + ".json?sort=" + Sort, _after, token, progress, true, null);
            _after = listing.Data.After;
            return listing;
        }

        public async void UpdateVotable(string name, int direction)
        {
            try
            {
                await Reddit.AddVote(name, direction);
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }
    }
}
