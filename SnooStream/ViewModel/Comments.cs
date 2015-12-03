﻿using GalaSoft.MvvmLight;
using SnooSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using SnooDom;
using System.Threading;
using SnooStream.Common;

namespace SnooStream.ViewModel
{
    public class CommentsViewModel : ObservableObject
    {
        public LoadViewModel LoadState { get; set; }
        public VotableViewModel Votable { get; set; }
        public ICommentBuilderContext Context { get; set; }
        public Link Thing { get; set; }
        public string Sort
        {
            get
            {
                return Context.Sort;
            }
            set
            {
                Context.Sort = value;
            }
        }

        public DateTime? LastRefresh { get; set; }
        public CollectionViewSource CommentsViewSource { get; set; }
        public ObservableCollection<object> Comments { get; set; }

        public CommentsViewModel()
        {

        }

        public CommentsViewModel(ICommentBuilderContext context)
        {
            Context = context;
            Comments = new ObservableCollection<object>();
            CommentsViewSource = new CollectionViewSource { Source = Comments };
            Thing = new Link();
            Votable = new VotableViewModel(Thing, Context.ChangeVote);
            Load();
        }

        public void Reload()
        {
            LoadState = LoadViewModel.ReplaceLoadViewModel(LoadState, new LoadViewModel { LoadAction = (progress, token) => ReloadAsync(progress, token), IsCritical = false });
            RaisePropertyChanged("LoadState");
        }

        public async Task ReloadAsync(IProgress<float> progress, CancellationToken token)
        {
            var comments = await Context.LoadRequested(true, progress, token);
            Context.Comments.Comments.Clear();
            Thing = comments.FirstOrDefault(thing => thing.Data is Link)?.Data as Link;
            RaisePropertyChanged("Thing");
            Votable.MergeVotable(Thing);
            CommentBuilder.FillFlatList(Context.Comments.Comments, comments.ToList(), Context);
        }

        public void Load()
        {
            LoadState = LoadViewModel.ReplaceLoadViewModel(LoadState, new LoadViewModel { LoadAction = (progress, token) => LoadAsync(progress, token), IsCritical = true });
            RaisePropertyChanged("LoadState");
        }

        public async Task LoadAsync(IProgress<float> progress, CancellationToken token)
        {
            Thing = await Context.GetLink(progress, token);
            RaisePropertyChanged("Thing");
            Votable.MergeVotable(Thing);
            var comments = await Context.LoadRequested(false, progress, token);
            CommentBuilder.FillFlatList(Context.Comments.Comments, comments.ToList(), Context);
        }
    }

    public class CommentViewModel : ObservableObject
    {
        public ICommentBuilderContext Context { get; set; }
        public Comment Thing { get; set; }
        public SnooDom.SnooDom BodyMD { get; set; }
        public Task<SnooDom.SnooDom> BodyMDTask { get; set; }
        public int Depth { get; set; }
        public VotableViewModel Votable { get; set; }
        public bool IsEditing { get; set; }
        internal bool _collapsed;
        public bool Collapsed
        {
            get
            {
                return _collapsed;
            }
            set
            {
                if (Set(ref _collapsed, value, "Collapsed"))
                {
                    if (value)
                        CommentBuilder.HideDecendents(Thing.Id, Context.Comments.Comments, Context);
                    else
                        CommentBuilder.ShowDecendents(this, Context.Comments.Comments, Context);
                }
            }
        }

        public async void Submit()
        {
            await SubmitAsync();
        }

        public async Task SubmitAsync()
        {
            if (!IsEditing)
                throw new InvalidOperationException("attempted to submit a comment that was not being edited");

            await Context.SubmitComment(this);
            IsEditing = false;
            RaisePropertyChanged("IsEditing");
        }
    }

    public class MoreViewModel : ObservableObject
    {
        public ICommentBuilderContext Context { get; set; }
        public LoadViewModel LoadState { get; set; }
        public List<string> Ids { get; set; }
        public int Count { get; set; }
        public int Depth { get; set; }

        public void LoadMore()
        {
            LoadState = LoadViewModel.ReplaceLoadViewModel(LoadState, new LoadViewModel { LoadAction = (progress, token) => LoadMoreAsync(progress, token), IsCritical = false });
            RaisePropertyChanged("LoadState");
        }

        public async Task LoadMoreAsync(IProgress<float> progress, CancellationToken token)
        {
            //need to watch for errors here in addition to hooking up the load progress object
            var comments = await Context.GetMore(Ids, progress, token);
            CommentBuilder.MergeMore(comments.ToList(), Context);
            Context.Comments.Comments.Remove(this);
            CommentBuilder.InsertIntoFlatList(Ids.FirstOrDefault(), Context.Comments.Comments, Context);
        }
    }

    public class LoadFullCommentsViewModel : ObservableObject
    {
        public ICommentBuilderContext Context { get; set; }
        public LoadViewModel LoadState { get; set; }
        public void LoadFully()
        {
            LoadState = LoadViewModel.ReplaceLoadViewModel(LoadState, new LoadViewModel { LoadAction = (progress, token) => LoadFullyAsync(progress, token), IsCritical = false });
            RaisePropertyChanged("LoadState");
        }

        public async Task LoadFullyAsync(IProgress<float> progress, CancellationToken token)
        {
            var comments = await Context.LoadAll(false, progress, token);
            Context.IsContextLoad = false;
            Context.Comments.Comments.Clear();
            CommentBuilder.FillFlatList(Context.Comments.Comments, comments.ToList(), Context);
        }
    }

    public enum CommentOriginType
    {
        PriorView,
        Edited,
        New,
        Context
    }

    //this class needs to take care of storing off prior comment sets so we can point the user directly to
    //the comments that have been either edited, added, or deleted
    public class CommentShell
    {
        public CommentOriginType OriginType { get; set; }
        public int InsertionWaveIndex { get; set; }
        public CommentViewModel Comment { get; set; }
        public string Id { get; set; }
        public string Parent { get; set; }
        public string NextSibling { get; set; }
        public string PriorSibling { get; set; }
        public string FirstChild { get; set; }
    }

    public interface ICommentBuilderContext
    {
        CommentsViewModel Comments { get; }
        string FirstChildName { get; set; }
        int InsertionWaveIndex { get; }
        CommentOriginType InsertionWaveType { get; }
        string CurrentUserName { get; }
        bool IsContextLoad { get; set; }
        bool IsDirty { get; }
        string Sort { get; set; }

        bool TryGetShell(string name, out CommentShell shell);
        bool IsKnownUnloaded(string name);
        void AddKnownUnloaded(string name, MoreViewModel priorMore);
        void RemoveKnownUnloaded(string name);
        bool IsMadeShell(string name);
        void AddMadeShell(string name, CommentShell shell);
        MoreViewModel GetUnloadedPlaceholder(string targetChild);
        //these need to return Listing instead so we can get cache date in
        //additionally these are a source of Link things 
        Task<IEnumerable<Thing>> GetMore(IEnumerable<string> ids, IProgress<float> progress, CancellationToken token);
        Task<IEnumerable<Thing>> LoadAll(bool ignoreCache, IProgress<float> progress, CancellationToken token);
        Task<IEnumerable<Thing>> LoadRequested(bool ignoreCache, IProgress<float> progress, CancellationToken token);
        Task SubmitComment(CommentViewModel viewModel);
        void PushOrigin(CommentOriginType type);
        void ChangeVote(string id, int direction);
        void PopOrigin();
        void RemoveShell(string id);
        void ClearState();
        SnooDom.SnooDom MakeMarkdown(string body);
        Task<Link> GetLink(IProgress<float> progress, CancellationToken token);
    }

    public class CommentBuilder
    {
        private static CommentShell LastChild(CommentShell shell, ICommentBuilderContext context)
        {
            if (shell.FirstChild != null)
            {
                CommentShell currentShell;
                if (context.TryGetShell(shell.FirstChild, out currentShell))
                {
                    return LastSibling(currentShell, context);
                }
            }
            return null;
        }
        private static CommentShell LastSibling(CommentShell shell, ICommentBuilderContext context)
        {
            var currentShell = shell;
            while (currentShell.NextSibling != null)
            {
                //this is the end as far as we're concerned
                CommentShell tmpShell;
                if (!context.TryGetShell(currentShell.NextSibling, out tmpShell))
                {
                    return currentShell;
                }
                else
                    currentShell = tmpShell;
            }
            return null;
        }

        private static void MergeComments(CommentShell parent, IEnumerable<Thing> things, ICommentBuilderContext context, int depth = 0)
        {
            CommentShell priorSibling = parent != null ? LastChild(parent, context) : null;
            MoreViewModel priorMore = null;
            foreach (var child in things)
            {
                if (child.Data is More && ((More)child.Data).Children.Count > 0)
                {
                    if (priorMore != null)
                    {
                        priorMore.Ids.AddRange(((More)child.Data).Children);
                        priorMore.Count += ((More)child.Data).Count;
                    }
                    else
                    {
                        var firstId = ((More)child.Data).Children.First();
                        if (priorSibling == null) //need to attach to parent
                        {
                            parent.FirstChild = firstId;
                        }
                        else
                        {
                            priorSibling.NextSibling = firstId;
                        }
                        if (!context.IsKnownUnloaded(firstId))
                        {
                            priorMore = new MoreViewModel { Context = context, Ids = ((More)child.Data).Children, Count = ((More)child.Data).Children.Count, Depth = depth };
                            context.AddKnownUnloaded(firstId, priorMore);
                        }
                    }
                }
                else if (child.Data is Comment)
                {
                    priorMore = null;
                    var commentId = ((Comment)child.Data).Id;

                    if (priorSibling == null && parent == null)
                    {
                        CommentShell firstChild;
                        if (context.FirstChildName != null && context.TryGetShell(context.FirstChildName, out firstChild))
                        {
                            priorSibling = LastSibling(firstChild, context);
                        }
                        else
                        {
                            context.FirstChildName = commentId;
                        }
                    }


                    if (priorSibling == null && parent != null) //need to attach to parent
                    {
                        parent.FirstChild = commentId;
                    }
                    else if (priorSibling != null)
                    {
                        priorSibling.NextSibling = commentId;
                    }


                    CommentShell current;
                    if (!context.IsMadeShell(commentId))
                    {
                        priorSibling = MakeCommentShell(child.Data as Comment, depth, priorSibling, context);
                        context.AddMadeShell(commentId, priorSibling);
                    }
                    else if (context.TryGetShell(commentId, out current))
                    {
                        current.PriorSibling = priorSibling != null ? priorSibling.Id : null;
                        current.NextSibling = null;
                        priorSibling = current;
                        MergeComment(priorSibling, child.Data as Comment, context);
                    }
                    else
                    {
                        throw new InvalidOperationException("comment was made but could not be found");
                    }

                    var replies = ((Comment)child.Data).Replies;
                    if (replies != null)
                        MergeComments(priorSibling, replies.Data.Children, context, depth + 1);
                }
            }
        }

        public static void FillFlatList(IList<object> targetCollection, IList<Thing> listing, ICommentBuilderContext context)
        {
            if (context.IsDirty)
                context.ClearState();

            var firstChild = listing.FirstOrDefault(thing => thing.Data is Comment);
            if (firstChild == null)
                return;

            context.PushOrigin(CommentOriginType.New);
            MergeComments(null, listing, context, 0);
            //add the load fully sentinel
            if (context.IsContextLoad)
                targetCollection.Add(new LoadFullCommentsViewModel { Context = context });

            InsertIntoFlatList(((Comment)firstChild.Data).Id, targetCollection, context);
        }

        public static IEnumerable<object> MergeMore(IList<Thing> listing, ICommentBuilderContext context)
        {
            List<object> flatChilden = new List<object>();
            FixupParentage(listing);
            var firstChild = listing.FirstOrDefault(thing => thing.Data is Comment);
            if (firstChild != null)
            {

                var parentId = ((Comment)firstChild.Data).ParentId;
                CommentShell parentShell;
                if (!context.TryGetShell(parentId.Replace("t1_", ""), out parentShell))
                {
                    parentShell = null;
                }

                MergeComments(parentShell, listing, context, parentShell == null ? 0 : parentShell.Comment.Depth + 1);
                var moreId = ((Comment)firstChild.Data).Id;
                InsertIntoFlatList(moreId, flatChilden, context);
                foreach (var child in flatChilden.OfType<CommentViewModel>())
                {
                    context.RemoveKnownUnloaded(GetId(child));
                }
            }
            return flatChilden;
        }

        private static void MergeComment(CommentShell priorSibling, Comment thingData, ICommentBuilderContext context)
        {
            priorSibling.OriginType = CommentOriginType.Edited;
            priorSibling.InsertionWaveIndex = context.InsertionWaveIndex - 1;
            priorSibling.Comment.Thing = thingData;
        }

        private static CommentShell MakeCommentShell(Comment comment, int depth, CommentShell priorSibling, ICommentBuilderContext context)
        {
            var result = new CommentShell
            {
                Comment = new CommentViewModel { Depth = depth, Thing = comment, Votable = new VotableViewModel(comment, context.ChangeVote), Context = context, _collapsed = false },
                Id = comment.Id,
                Parent = comment.ParentId.StartsWith("t1_") ? comment.ParentId.Substring("t1_".Length) : null,
                PriorSibling = priorSibling != null ? priorSibling.Id : null,
                InsertionWaveIndex = context.InsertionWaveIndex - 1,
                OriginType = context.InsertionWaveType
            };
            return result;
        }

        //this needs to be called on anything that comes back from a load more because reddit mostly just gives us a non structured listing of comments
        private static void FixupParentage(IList<Thing> listing)
        {
            Dictionary<string, Comment> nameMap = new Dictionary<string, Comment>();
            foreach (var item in listing)
            {
                if (item.Data is Comment)
                    nameMap.Add(((Comment)item.Data).Name, ((Comment)item.Data));
            }

            foreach (var item in new List<Thing>(listing))
            {
                string parentId = null;
                if (item.Data is Comment)
                    parentId = ((Comment)item.Data).ParentId;
                else if (item.Data is More)
                    parentId = ((More)item.Data).ParentId;

                if (parentId != null && nameMap.ContainsKey(parentId))
                {
                    var targetParent = nameMap[parentId];
                    if (targetParent.Replies == null)
                        targetParent.Replies = new Listing { Data = new ListingData { Children = new List<Thing>() } };

                    targetParent.Replies.Data.Children.Add(item);
                    listing.Remove(item);
                }
            }
        }

        public static void InsertIntoFlatList(string firstChild, IList<object> targetContainer, ICommentBuilderContext context)
        {
            CommentShell childShell;
            var targetChild = firstChild;
            while (targetChild != null)
            {
                if (context.TryGetShell(targetChild, out childShell))
                {
                    targetContainer.Add(childShell.Comment);
                    if (childShell.FirstChild != null)
                    {
                        InsertIntoFlatList(childShell.FirstChild, targetContainer, context);
                    }
                    targetChild = childShell.NextSibling;
                }
                else if (context.IsKnownUnloaded(targetChild)) //if its not in the list check the known unloaded list (more)
                {
                    targetContainer.Add(context.GetUnloadedPlaceholder(targetChild));
                    targetChild = null;
                }
                else //we must be looking at something missing because on context so we need to put out a 'loadfull' viewmodel
                {
                    targetContainer.Add(new LoadFullCommentsViewModel { Context = context });
                    targetChild = null;
                }
            }
        }

        private static string GetId(object viewModel)
        {
            if (viewModel is CommentViewModel)
                return ((CommentViewModel)viewModel).Thing.Id;
            else if (viewModel is MoreViewModel)
                return ((MoreViewModel)viewModel).Ids.First();
            else
                return "";
        }

        private static Comment MakeNewComment(string parentId, int depth, ICommentBuilderContext context)
        {
            var newCommentId = UnusedCommentId(context);
            return new Comment
            {
                Author = context.CurrentUserName,
                CreatedUTC = DateTime.UtcNow,
                LinkTitle = context.Comments.Thing.Title,
                LinkId = context.Comments.Thing.Name,
                Body = "",
                Name = "t1_" + newCommentId,
                Id = newCommentId,
                Ups = 1,
                Downs = 0,
                LinkUrl = context.Comments.Thing.Url,
                ParentId = parentId ?? context.Comments.Thing.Name,
                Replies = new Listing { Data = new ListingData { Children = new List<Thing>() } },
                Subreddit = context.Comments.Thing.Subreddit,
                Likes = true,
                Created = DateTime.Now
            };
        }

        public static void MergeDisplayChildren(IList<object> flatComments, IEnumerable<ViewModelBase> newChildren, IEnumerable<string> replaceId)
        {
            //the children will be in a contiguous block so we just need to find the existing viewmodel that 
            //matches the afterId then we can add each one in series
            bool foundFirst = false;
            for (int i = 0; i < flatComments.Count; i++)
            {
                var commentId = GetId(flatComments[i]);
                if (replaceId.Contains(commentId))
                {
                    flatComments.RemoveAt(i);
                    if (!foundFirst)
                    {
                        foreach (var child in newChildren)
                        {
                            if ((flatComments.Count - 1) <= i)
                                flatComments.Add(child);
                            else
                                flatComments.Insert(i, child);

                            i++;
                        }
                    }
                    foundFirst = true; ;
                }
            }
        }

        private static string UnusedCommentId(ICommentBuilderContext context)
        {
            for (int i = 0; ; i++)
            {
                if (!context.IsMadeShell(i.ToString()))
                    return i.ToString();
            }
        }

        public static CommentViewModel AddReplyComment(string parentId, IList<object> targetList, ICommentBuilderContext context)
        {
            Comment newComment = null;
            CommentShell parent = null;
            if (parentId == null)
            {
                newComment = MakeNewComment(parentId, 0, context);
            }
            else
            {
                var searchId = parentId.Substring("t1_".Length);
                if (context.TryGetShell(searchId, out parent))
                {
                    newComment = MakeNewComment(parentId, parent.Comment.Depth, context);
                }
                else
                    throw new ArgumentOutOfRangeException();
            }

            context.PushOrigin(CommentOriginType.New);
            try
            {
                if (parent == null)
                {
                    var commentShell = MakeCommentShell(newComment, 0, null, context);
                    context.AddMadeShell(newComment.Id, commentShell);
                    if (context.FirstChildName != null)
                    {
                        CommentShell firstChild;
                        if (context.TryGetShell(context.FirstChildName, out firstChild))
                            firstChild.PriorSibling = newComment.Id;
                        else
                            throw new InvalidOperationException("first child was non null but could not be found");
                    }

                    commentShell.NextSibling = context.FirstChildName;
                    context.FirstChildName = newComment.Id;
                    targetList.Insert(0, commentShell.Comment);
                    commentShell.Comment.IsEditing = true;
                    return commentShell.Comment;
                }
                else
                {
                    var commentShell = MakeCommentShell(newComment, parent.Comment.Depth + 1, null, context);
                    context.AddMadeShell(newComment.Id, commentShell);
                    var firstSiblingId = parent.FirstChild;
                    commentShell.NextSibling = parent.FirstChild;
                    parent.FirstChild = commentShell.Id;
                    if (parent.Comment.Thing.Replies != null)
                        parent.Comment.Thing.Replies.Data.Children.Add(new Thing { Kind = "t1", Data = newComment });
                    else
                        parent.Comment.Thing.Replies = new Listing { Data = new ListingData { Children = new List<Thing> { new Thing { Kind = "t1", Data = newComment } } } };

                    CommentShell firstSiblingShell;
                    if (firstSiblingId != null && context.TryGetShell(firstSiblingId, out firstSiblingShell))
                    {
                        firstSiblingShell.PriorSibling = newComment.Id;
                    }
                    targetList.Insert(targetList.IndexOf(parent.Comment) + 1, commentShell.Comment);
                    commentShell.Comment.IsEditing = true;
                    return commentShell.Comment;
                }
            }
            finally
            {
                context.PopOrigin();
            }

        }

        public static void RemoveComment(CommentViewModel commentViewModel, IList<object> targetList, ICommentBuilderContext context)
        {
            CommentShell commentShell;
            if (context.TryGetShell(commentViewModel.Thing.Id, out commentShell))
            {
                CommentShell parentShell = null;
                context.TryGetShell(commentViewModel.Thing.ParentId, out parentShell);
                if (parentShell == null)
                {
                    if (commentShell.PriorSibling == null)
                        context.FirstChildName = commentShell.NextSibling;
                }
                else
                {
                    if (commentShell.PriorSibling == null)
                        parentShell.FirstChild = commentShell.NextSibling;
                }

                if (commentShell.PriorSibling != null)
                {
                    CommentShell priorSibling;
                    if (context.TryGetShell(commentShell.PriorSibling, out priorSibling))
                        priorSibling.NextSibling = commentShell.NextSibling;
                }

                if (commentShell.NextSibling != null)
                {
                    CommentShell nextSibling;
                    if (context.TryGetShell(commentShell.NextSibling, out nextSibling))
                        nextSibling.PriorSibling = commentShell.PriorSibling;
                    else
                        throw new InvalidOperationException("nextsibling was non null but could not be found");
                }

                context.RemoveShell(commentViewModel.Thing.Id);
            }
            targetList.Remove(commentViewModel);
        }

        public static void HideDecendents(string id, IList<object> targetList, ICommentBuilderContext context)
        {
            foreach (var vmb in Decendents(id, context))
            {
                targetList.Remove(vmb);
            }
        }

        public static void ShowDecendents(CommentViewModel parent, IList<object> targetList, ICommentBuilderContext context)
        {
            var insertionPoint = targetList.IndexOf(parent) + 1;
            foreach (var vmb in Decendents(parent.Thing.Id, context).Reverse())
            {
                targetList.Insert(insertionPoint, vmb);
            }
        }

        public static IEnumerable<object> Decendents(string id, ICommentBuilderContext context)
        {
            var result = new List<object>();

            CommentShell currentShell;
            if (context.TryGetShell(id, out currentShell) && currentShell.FirstChild != null)
            {
                CommentShell tmpShell;
                if (!context.TryGetShell(currentShell.FirstChild, out tmpShell))
                {
                    MoreViewModel moreVm;
                    if ((moreVm = context.GetUnloadedPlaceholder(currentShell.FirstChild)) != null)
                    {
                        result.Add(moreVm);
                    }
                    return result;
                }
                else
                    currentShell = tmpShell;
            }
            else
                return result;

            result.Add(currentShell.Comment);
            if (currentShell.FirstChild != null)
                result.AddRange(Decendents(currentShell.Id, context));


            while (currentShell.NextSibling != null)
            {

                CommentShell tmpShell;
                if (!context.TryGetShell(currentShell.NextSibling, out tmpShell))
                {
                    //this is the end as far as we're concerned
                    MoreViewModel moreValues;
                    if ((moreValues = context.GetUnloadedPlaceholder(currentShell.NextSibling)) != null)
                    {
                        result.Add(moreValues);
                    }
                    break;
                }
                else
                {
                    currentShell = tmpShell;
                    result.Add(currentShell.Comment);
                    if (currentShell.FirstChild != null)
                        result.AddRange(Decendents(currentShell.Id, context));
                }
            }
            return result;
        }
    }

    public abstract class BasicCommentBuilderContext : ICommentBuilderContext
    {
        public abstract void ChangeVote(string id, int direction);
        public abstract Task<IEnumerable<Thing>> GetMore(IEnumerable<string> ids, IProgress<float> progress, CancellationToken token);
        public abstract Task<IEnumerable<Thing>> LoadAll(bool ignoreCache, IProgress<float> progress, CancellationToken token);
        public abstract Task<IEnumerable<Thing>> LoadRequested(bool ignoreCache, IProgress<float> progress, CancellationToken token);
        public abstract Task SubmitComment(CommentViewModel viewModel);
        public abstract string CurrentUserName { get; }
        public abstract Task<Link> GetLink(IProgress<float> progress, CancellationToken token);

        private SnooDom.SimpleSessionMemoryPool _markdownMemoryPool;

        private string _sort;
        public string Sort
        {
            get
            {
                return _sort;
            }
            set
            {
                if (_sort != value)
                {
                    IsDirty = true;
                    _sort = value;
                }
            }
        }

        public bool IsDirty { get; set; }

        //every time something gets merged in we push a new item on the end of the list
        //every comment shell gets made with the index to its creation origin so the converter can
        //come through later when we're displaying this stuff and determine how we need to show the changes
        //to the user
        List<CommentOriginType> _commentOriginStack = new List<CommentOriginType>();

        private Dictionary<string, CommentShell> _comments = new Dictionary<string, CommentShell>();
        private Dictionary<string, MoreViewModel> _knownUnloaded = new Dictionary<string, MoreViewModel>();

        public CommentsViewModel Comments { get; set; }
        public string FirstChildName { get; set; }
        public bool IsContextLoad { get; set; }

        public int InsertionWaveIndex
        {
            get
            {
                return _commentOriginStack.Count;
            }
        }

        public CommentOriginType InsertionWaveType
        {
            get
            {
                return _commentOriginStack.LastOrDefault();
            }
        }

        public void ClearState()
        {
            _commentOriginStack.Clear();
            _comments.Clear();
            _knownUnloaded.Clear();
            _markdownMemoryPool = new SnooDom.SimpleSessionMemoryPool();
        }

        public void AddKnownUnloaded(string name, MoreViewModel priorMore)
        {
            _knownUnloaded.Add(name, priorMore);
        }

        public void AddMadeShell(string name, CommentShell shell)
        {
            _comments.Add(name, shell);
        }

        public MoreViewModel GetUnloadedPlaceholder(string targetChild)
        {
            return _knownUnloaded[targetChild];
        }

        public bool IsKnownUnloaded(string name)
        {
            return _knownUnloaded.ContainsKey(name);
        }

        public bool IsMadeShell(string name)
        {
            return _comments.ContainsKey(name);
        }

        public void PushOrigin(CommentOriginType type)
        {
            _commentOriginStack.Add(type);
        }

        public void PopOrigin()
        {
            _commentOriginStack.Remove(_commentOriginStack.LastOrDefault());
        }

        public void RemoveKnownUnloaded(string name)
        {
            _knownUnloaded.Remove(name);
        }

        public bool TryGetShell(string name, out CommentShell shell)
        {
            return _comments.TryGetValue(name, out shell);
        }

        public void RemoveShell(string id)
        {
            _comments.Remove(id);
        }

        public SnooDom.SnooDom MakeMarkdown(string body)
        {
            return SnooDom.SnooDom.MarkdownToDOM(body, _markdownMemoryPool);
        }
    }

    class CommentBuilderContext : BasicCommentBuilderContext
    {
        public Reddit Reddit { get; set; }
        public string Url { get; set; }
        public string LinkName { get; set; }
        public string PermaLink { get; set; }
        public string Subreddit { get; set; }
        public string ContextTarget { get; set; }
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

        public override async Task<IEnumerable<Thing>> GetMore(IEnumerable<string> ids, IProgress<float> progress, CancellationToken token)
        {
            var moreListing = await Reddit.GetMoreOnListing(new More { Children = ids.ToList(), Count = ids.Count() }, LinkName, Subreddit, token, progress);
            return moreListing.Data.Children;
        }

        public override async Task<IEnumerable<Thing>> LoadAll(bool ignoreCache, IProgress<float> progress, CancellationToken token)
        {
            return (await Reddit.GetCommentsOnPost(Subreddit, PermaLink, token, progress, true, sort: Sort))?.Data?.Children;
        }

        public override async Task<IEnumerable<Thing>> LoadRequested(bool ignoreCache, IProgress<float> progress, CancellationToken token)
        {
            return (await Reddit.GetCommentsOnPost(Subreddit, PermaLink, token, progress, true, null, ContextTarget, Sort))?.Data?.Children;
        }

        public override async Task SubmitComment(CommentViewModel viewModel)
        {
            if (viewModel.IsEditing)
            {
                await Reddit.EditComment(viewModel.Thing.Id, viewModel.Thing.Body);
            }
            else
            {
                await Reddit.AddComment(viewModel.Thing.ParentId, viewModel.Thing.Body);
            }
        }

        public override async Task<Link> GetLink(IProgress<float> progress, CancellationToken token)
        {
            if (Navigation.CommentRegex.IsMatch(Url) ||
                Navigation.CommentsPageRegex.IsMatch(Url))
            {
                var foundLink = (await Reddit.GetLinkByUrl(Url, token, progress, false))?.Data as Link;
                PermaLink = foundLink?.Permalink;
                LinkName = foundLink?.Name;
                Subreddit = foundLink?.Subreddit;
                return foundLink;
            }
            else if (Navigation.ShortCommentsPageRegex.IsMatch(Url))
            {
                var thingId = "t3_" + Url.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Last();
                var foundLink = (await Reddit.GetThingById(thingId, token, progress, false))?.Data as Link;
                PermaLink = foundLink?.Permalink;
                LinkName = foundLink?.Name;
                Subreddit = foundLink?.Subreddit;
                return foundLink;
            }
            else
            {
                throw new InvalidOperationException("failed to convert url to a usable Thing<Link>");
            }
        }
    }
}
