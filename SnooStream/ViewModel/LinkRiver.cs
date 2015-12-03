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

namespace SnooStream.ViewModel
{
    public class LinkRiverViewModel
    {
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

        private async Task<LoadMoreItemsResult> LoadItem(LoadViewModel loadItem)
        {
            var itemCount = Count - 1;
            await loadItem.LoadAsync();
            //now that the load is finished the load item should be removed from the list
            Remove(loadItem);
            return new LoadMoreItemsResult { Count = (uint)(Count - itemCount) };
        }
    }

    public interface ILinkBuilderContext
    {
        void UpdateVotable(string name, int direction);
        Task<Dictionary<string, LinkMeta>> GenerateLinkMeta(IEnumerable<string> linkNames);
        void HandleError(Exception ex);
        bool HasAdditional { get; }
        Task<Listing> Load(IProgress<float> progress, CancellationToken token, bool ignoreCache);
        Task<Listing> LoadAdditional(IProgress<float> progress, CancellationToken token);
        bool IsMultiReddit { get; }
    }

    public static class LinkBuilder
    {
        public static IEnumerable<LinkViewModel> MakeLinkViewModels(IEnumerable<Thing> things, ILinkBuilderContext builderContext)
        {
            var madeViewModels = things.Select(thing => new LinkViewModel { Thing = thing.Data as Link, Votable = new VotableViewModel(thing.Data as Link, builderContext.UpdateVotable), CommentCount = ((Link)thing.Data).CommentCount, FromMultiReddit = builderContext.IsMultiReddit }).ToList();
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
        public Link Thing { get; set; }
        public VotableViewModel Votable { get; set; }
        public LinkMeta Metadata { get; set; }
        public int CommentCount { get; set; }
        public int ReadCommentCount { get; set; }
        public bool FromMultiReddit { get; set; }

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

        public void UpdateMetadata(LinkMeta linkMeta)
        {
            Metadata = linkMeta;
            if (linkMeta.CommentCount != ReadCommentCount)
            {
                ReadCommentCount = linkMeta.CommentCount;
                RaisePropertyChanged("ReadCommentCount");
            }
        }

        public void GotoComments()
        {

        }

        public void Share()
        {

        }

        public void Hide()
        {

        }

        public void Report()
        {

        }

        public void Save()
        {

        }

        public void GotoUserDetails()
        {

        }
    }

    class LinkBuilderContext : ILinkBuilderContext
    {
        public string Subreddit { get; set; }
        public string Sort { get; set; }
        public SnooSharp.Reddit Reddit { get; set; }
        public OfflineService Offline { get; set; }
        private string _after;
        private bool _hasCollectionLoadViewModel = false;

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
                return !string.IsNullOrWhiteSpace(_after) && !_hasCollectionLoadViewModel;
            }
        }

        public Task<Dictionary<string, LinkMeta>> GenerateLinkMeta(IEnumerable<string> linkNames)
        {
            return Task.Run(async () =>
            {
                return (await Offline.GetLinkMetadata(linkNames)).ToDictionary(lm => ((ThingData)lm.Data).Name);
            });
        }

        public void HandleError(Exception ex)
        {
            //TODO proper error noticiation here
            Debug.WriteLine(ex);
        }

        public async Task<Listing> Load(IProgress<float> progress, CancellationToken token, bool ignoreCache)
        {
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
