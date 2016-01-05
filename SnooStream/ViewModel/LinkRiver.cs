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

namespace SnooStream.ViewModel
{
    public class LinkRiverViewModel : IHasTitle
    {
        public string Title { get { return Context.Title; } }
        public ILinkBuilderContext Context { get; set; }
        public Subreddit Thing { get; set; }
        public string Sort { get; set; }
        public DateTime? LastRefresh { get; set; }
        public CollectionViewSource LinkViewSource { get; set; }
        public ObservableCollection<object> Links {get; set;}
        public string LastLinkId { get; set; }

        public LinkRiverViewModel()
        {

        }

        public LinkRiverViewModel(ILinkBuilderContext context)
        {
            Context = context;
            LinkViewSource = new CollectionViewSource();
            Links = new LinkCollection { Context = context, LinkRiver = this };
            LinkViewSource.Source = Links;
        }

        public bool IsUserMultiReddit
        {
            get
            {
                if (Thing == null || Thing.Url == "/")
                    return false;
                else
                    return Thing.Url.Contains("/m/") || Thing.Url.Contains("+");
            }
        }

        public bool IsModerator
        {
            get
            {
                if (Thing == null)
                    return false;
                else
                    return Thing.Moderator;
            }
        }

        public bool IsMultiReddit
        {
            get
            {
                if (Thing == null || Thing.Url == "/")
                    return true;
                else
                    return Thing.Url.Contains("/m/") || Thing.Url.Contains("+");
            }
        }

        public async Task<IEnumerable<LinkViewModel>> LoadAsync(IProgress<float> progress, CancellationToken token, bool ignoreCache)
        {
            var activityListing = await Context.Load(progress, token, ignoreCache);
            LastRefresh = activityListing.DataAge ?? DateTime.UtcNow;
            return LinkBuilder.AppendLinkViewModels(Links, activityListing.Data.Children, Context);
        }

        public async Task<IEnumerable<LinkViewModel>> LoadAdditionalAsync(IProgress<float> progress, CancellationToken token)
        {
            var activityListing = await Context.LoadAdditional(progress, token);
            return LinkBuilder.AppendLinkViewModels(Links, activityListing.Data.Children, Context);
        }
    }

    public class LinkCollection : ObservableCollection<object>, ISupportIncrementalLoading
    {
        public ILinkBuilderContext Context { get; set; }
        public LinkRiverViewModel LinkRiver { get; set; }
        public bool HasMoreItems
        {
            get
            {
                return Context.HasAdditional;
            }
        }

        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            //Load Additional
            if (Count > 0)
            {
                var loadItem = new LoadViewModel { LoadAction = (progress, token) => LinkRiver.LoadAdditionalAsync(progress, token), IsCritical = false };
                Add(loadItem);
                return LoadItem(loadItem).AsAsyncOperation();
            }
            else //Load fresh
            {
                var loadItem = new LoadViewModel { LoadAction = (progress, token) => LinkRiver.LoadAsync(progress, token, false), IsCritical = true };
                Add(loadItem);
                return LoadItem(loadItem).AsAsyncOperation();
            }
        }

        private void AddRange(IEnumerable<LinkViewModel> collection)
        {
            foreach (var item in collection)
                Add(item);
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

    public interface ILinkContext
    {
        bool IsHighBandwidth { get; }
        string CurrentUser { get; }
        void GotoComments(LinkViewModel linkViewModel);
        Task<Thing> GetThing(string url);
        void UpdateVotable(string name, int direction);
        void GotoLink(string url);
        void Share(LinkViewModel linkViewModel);
        void Hide(LinkViewModel linkViewModel);
        void Report(string id);
        void Save(string id);
        void Delete(LinkViewModel linkViewModel);
        void SubmitEdit(MarkdownEditingViewModel editing);
        void GotoUserDetails(string author);
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
        bool IsMultiReddit { get; }
        ILinkContext LinkContext { get; }
    }

    public static class LinkBuilder
    {
        public static async Task<LinkViewModel> MakeLinkViewModel(string url, ILinkContext context)
        {
            var madeThing = await context.GetThing(url);
            var linkViewModel = new LinkViewModel { Context = context, Thing = madeThing.Data as Link, Votable = new VotableViewModel(madeThing.Data as Link, context.UpdateVotable), CommentCount = ((Link)madeThing.Data).CommentCount, FromMultiReddit = false };
            return linkViewModel;
        }

        public static IEnumerable<LinkViewModel> MakeLinkViewModels(IEnumerable<Thing> things, ILinkBuilderContext builderContext)
        {
            var madeViewModels = things.Select(thing => new LinkViewModel { Context = builderContext.LinkContext, Thing = thing.Data as Link, Votable = new VotableViewModel(thing.Data as Link, builderContext.UpdateVotable), CommentCount = ((Link)thing.Data).CommentCount, FromMultiReddit = builderContext.IsMultiReddit }).ToList();
            UpdateLinkMetadata(things, builderContext, madeViewModels);
            return madeViewModels;
        }

        private static void UpdateLinkMetadata(IEnumerable<Thing> things, ILinkBuilderContext builderContext, IEnumerable<LinkViewModel> madeViewModels)
        {
            var metadataTask = builderContext.GenerateLinkMeta(things.Select(thing => ((Link)thing.Data).Name));
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

            var madeViewModels = things.Select(thing => new LinkViewModel { Thing = thing.Data as Link, Votable = new VotableViewModel(thing.Data as Link, builderContext.UpdateVotable), CommentCount = ((Link)thing.Data).CommentCount }).ToList();
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

    public class LinkViewModel : ObservableObject
    {
        public ILinkContext Context { get; set; }
        public Link Thing { get; set; }
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

            Thing = thing;
            Votable.MergeVotable(thing);
            
        }

        private PreviewImage Preview
        {
            get
            {
                if (Thing.Preview?.images != null)
                {
                    var previewImages = Thing.Preview.images.OrderBy(thing => thing.source.height * thing.source.width);
                    if (Context.IsHighBandwidth)
                        return previewImages.FirstOrDefault();
                    else
                        return previewImages.LastOrDefault();
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
                return Preview?.source.url;
            }
        }

        public double PreviewHeight
        {
            get
            {
                return Preview?.source.height ?? 0.0;
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
            Context.GotoLink(Thing.Url);
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
            Context.GotoUserDetails(Thing.Author);
        }
    }

    class LinkContext : ILinkContext
    {
        public LinkRiverViewModel LinkRiver { get; set; }
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

        public void GotoComments(LinkViewModel link)
        {
            Navigation.GotoComments(link.Thing.Permalink, NavigationContext, link);
        }

        public Task<Thing> GetThing(string url)
        {
            throw new NotImplementedException();
        }

        public async void UpdateVotable(string name, int direction)
        {
            try
            {
                await Reddit.AddVote(name, direction);
            }
            catch { }
        }

        public void GotoLink(string url)
        {
            Navigation.GotoLink(LinkRiver, url, NavigationContext);
        }

        public void Share(LinkViewModel linkViewModel)
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

        public void Hide(LinkViewModel linkViewModel)
        {
            LinkRiver.Links.Remove(linkViewModel);
            Reddit.HideThing(linkViewModel.Thing.Id);
        }

        public void Report(string id)
        {
            Reddit.AddReportOnThing(id);
        }

        public void Save(string id)
        {
            Reddit.AddSavedThing(id);
        }

        public async void Delete(LinkViewModel linkViewModel)
        {
            LinkRiver.Links.Remove(linkViewModel);
            try
            {
                await Reddit.DeleteLinkOrComment(linkViewModel.Thing.Id);
            }
            catch { }
        }

        public async void SubmitEdit(MarkdownEditingViewModel editing)
        {
            try
            {
                await Reddit.EditPost(editing.Text, editing.Context.TargetName);
            }
            catch { }
        }

        public void GotoUserDetails(string author)
        {
            Navigation.GotoUserDetails(author, NavigationContext);
        }
    }

    class LinkBuilderContext : ILinkBuilderContext
    {
        public string Title { get { return Subreddit; } }
        public ILinkContext LinkContext { get; set; }
        public string Subreddit { get; set; }
        public string Sort { get; set; }
        public SnooSharp.Reddit Reddit { get; set; }
        public OfflineService Offline { get; set; }
        private string _after;
        private bool _hasLoaded = false;

        public bool IsMultiReddit
        {
            get
            {
                return Subreddit.Contains("/m/") || Subreddit.Contains("+");
            }
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

        public async Task<Listing> Load(IProgress<float> progress, CancellationToken token, bool ignoreCache)
        {
            _hasLoaded = true;
            var listing = await Reddit.GetPostsBySubreddit(Subreddit, token, progress, ignoreCache, Sort, null);
            _after = listing.Data.After;
            return listing;
        }

        public async Task<Listing> LoadAdditional(IProgress<float> progress, CancellationToken token)
        {
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
