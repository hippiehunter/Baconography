using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SnooSharp;
using SnooStream.Common;
using SnooStream.ViewModel.Popups;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public enum CommentOriginType
    {
        PriorView,
        Edited,
        New,
        Context
    }
    
    //this class needs to take care of storing off prior comment sets so we can point the user directly to
    //the comments that have been either edited, added, or deleted
    public class CommentsViewModel : ViewModelBase, IRefreshable, IHasFocus
    {
        private class CommentShell
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

        //every time something gets merged in we push a new item on the end of the list
        //every comment shell gets made with the index to its creation origin so the converter can
        //come through later when we're displaying this stuff and determine how we need to show the changes
        //to the user
        List<CommentOriginType> _commentOriginStack = new List<CommentOriginType>();
        
        private Dictionary<string, CommentShell> _comments = new Dictionary<string, CommentShell>();
		private Dictionary<string, MoreViewModel> _knownUnloaded = new Dictionary<string, MoreViewModel>();
        private string _firstChild;
        private LoadFullCommentsViewModel _loadFullSentinel;
        private ViewModelBase _context;

        public CommentsViewModel(ViewModelBase context, Link linkData)
        {
            _context = context;
            Link = _context as LinkViewModel;
            _loadFullSentinel = new LoadFullCommentsViewModel(this);
            FlatComments = new ObservableCollection<ViewModelBase>();
            ProcessUrl((linkData.Permalink.Contains("://") && linkData.Permalink.Contains("reddit.com")) ?
				linkData.Permalink :
				"http://www.reddit.com" + linkData.Permalink);
			_commentsContentStream = new Lazy<CommentsContentStreamViewModel>(() => new CommentsContentStreamViewModel(this));
        }

        public CommentsViewModel(ViewModelBase context, string url)
        {
            _context = context;
            Link = _context as LinkViewModel;
            _loadFullSentinel = new LoadFullCommentsViewModel(this);
            FlatComments = new ObservableCollection<ViewModelBase>();
            ProcessUrl(url);
			_commentsContentStream = new Lazy<CommentsContentStreamViewModel>(() => new CommentsContentStreamViewModel(this));
        }

		public CommentsViewModel(ViewModelBase context, Listing comments, string url, DateTime? lastRefresh, bool isLimitedContext = false)
		{
			LastRefresh = lastRefresh;
			_context = context;
			Link = _context as LinkViewModel;
			_loadFullSentinel = new LoadFullCommentsViewModel(this);
			FlatComments = new ObservableCollection<ViewModelBase>();
			ProcessUrl(url);
			_loadFullTask = new Lazy<Task>(() => Task.FromResult(true));
			LoadDump(comments);
			_commentsContentStream = new Lazy<CommentsContentStreamViewModel>(() => new CommentsContentStreamViewModel(this));
		}

        private string UnusedCommentId()
        {
            for (int i = 0; ; i++)
            {
                if (!_comments.ContainsKey(i.ToString()))
                    return i.ToString();
            }
        }

        public CommentViewModel AddReplyComment(string parentId)
        {
            Comment newComment = null;
            CommentShell parent = null;
            if (parentId == null)
            {
                newComment = MakeNewComment(parentId, 0);
            }
            else
            {
                var searchId = parentId.Substring("t1_".Length);
                if (_comments.TryGetValue(searchId, out parent))
                {
                    newComment = MakeNewComment(parentId, parent.Comment.Depth);
                }
                else
                    throw new ArgumentOutOfRangeException();
            }

            _commentOriginStack.Add(CommentOriginType.New);
            try
            {
                if (parent == null)
                {
                    var commentShell = MakeCommentShell(newComment, 0, null);
                    _comments.Add(newComment.Id, commentShell);
                    if (_firstChild != null)
                        _comments[_firstChild].PriorSibling = newComment.Id;

                    commentShell.NextSibling = _firstChild;
                    _firstChild = newComment.Id;
                    FlatComments.Insert(0, commentShell.Comment);
                    commentShell.Comment.IsEditing = true;
                    return commentShell.Comment;
                }
                else
                {
                    var commentShell = MakeCommentShell(newComment, parent.Comment.Depth + 1, null);
                    _comments.Add(newComment.Id, commentShell);
                    var firstSiblingId = parent.FirstChild;
                    commentShell.NextSibling = parent.FirstChild;
                    parent.FirstChild = commentShell.Id;
                    if (parent.Comment.Thing.Replies != null)
                        parent.Comment.Thing.Replies.Data.Children.Add(new Thing { Kind = "t1", Data = newComment });
                    else
                        parent.Comment.Thing.Replies = new Listing { Data = new ListingData { Children = new List<Thing> { new Thing { Kind = "t1", Data = newComment } } } };

                    CommentShell firstSiblingShell;
                    if (firstSiblingId != null && _comments.TryGetValue(firstSiblingId, out firstSiblingShell))
                    {
                        firstSiblingShell.PriorSibling = newComment.Id;
                    }
                    FlatComments.Insert(FlatComments.IndexOf(parent.Comment) + 1, commentShell.Comment);
                    commentShell.Comment.IsEditing = true;
                    return commentShell.Comment;
                }
            }
            finally
            {
                _commentOriginStack.Clear();
            }
            
        }

        public void RemoveComment(CommentViewModel commentViewModel)
        {
            var commentShell = _comments[commentViewModel.Id];
            var parentShell = commentViewModel.Parent != null ? _comments[commentViewModel.Parent.Id] : null;
            
            if (parentShell == null)
            {   
                if(commentShell.PriorSibling == null)
                    _firstChild = commentShell.NextSibling;
            }
            else
            {
                if(commentShell.PriorSibling == null)
                    parentShell.FirstChild = commentShell.NextSibling;   
            }

            if (commentShell.PriorSibling != null)
                _comments[commentShell.PriorSibling].NextSibling = commentShell.NextSibling;

            if (commentShell.NextSibling != null)
                _comments[commentShell.NextSibling].PriorSibling = commentShell.PriorSibling;

            _comments.Remove(commentViewModel.Id);
            FlatComments.Remove(commentViewModel);
        }

        //does not deal with renaming for parentage
        internal void RenameThing(string currentId, string newId)
        {
            var commentShell = _comments[currentId];
            if (commentShell.Parent == null)
            {
                if (commentShell.PriorSibling == null)
                    _firstChild = newId;
            }
            else
            {
                if (commentShell.PriorSibling == null)
                    _comments[commentShell.Parent].FirstChild = newId;
            }

            if(commentShell.PriorSibling != null)
                _comments[commentShell.PriorSibling].NextSibling = newId;

            if (commentShell.NextSibling != null)
                _comments[commentShell.NextSibling].PriorSibling = newId;

            commentShell.Id = newId;
            commentShell.Comment.Id = newId;
            _comments.Remove(currentId);
            _comments.Add(newId, commentShell);
        }

        private Comment MakeNewComment(string parentId, int depth)
        {
            var newCommentId = UnusedCommentId();
            return new Comment
            {
                Author = SnooStreamViewModel.RedditService.CurrentUserName,
                CreatedUTC = DateTime.UtcNow,
                LinkTitle = Link.Title,
                LinkId = Link.Name,
                Body = "",
                Name = "t1_" + newCommentId,
                Id = newCommentId,
                Ups = 1,
                Downs = 0,
                LinkUrl = Link.Url,
                ParentId = parentId ?? Link.Name,
                Replies = new Listing { Data = new ListingData { Children = new List<Thing>() } },
                Subreddit = Link.Subreddit,
                Likes = true,
                Created = DateTime.Now
            };
        }

        Lazy<CommentsContentStreamViewModel> _commentsContentStream;
		public CommentsContentStreamViewModel CommentsContentStream
		{
			get
			{
				return _commentsContentStream.Value;
			}
		}

		public CommentViewModel GetById (string id)
		{
			CommentShell shell;
			if (_comments.TryGetValue(id, out shell) && shell != null)
				return shell.Comment;
			else
				return null;
		}

        private void ProcessUrl(string url)
        {
            Uri uri;
            if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri))
            {
                var queryParts = uri.Query.Split('&', '?')
                    .Where(str => str != null && str.Contains("="))
                    .Select(str => str.Split('='))
                    .ToDictionary(arr => arr[0].ToLower() , arr => arr[1]);

                if (queryParts.ContainsKey("sort"))
                    Sort = queryParts["sort"];
                else
                    Sort = "confidence";

                BaseUrl = url.Substring(0, url.Length - uri.Query.Length);
                var lastSlash = BaseUrl.LastIndexOf('/');
                if (queryParts.ContainsKey("context") || lastSlash < BaseUrl.Length - 1)
                {
                    IsContext = true;
                }
                else
                    IsContext = false;

                if (IsContext && lastSlash > -1)
                {
                    ContextTargetID = BaseUrl.Substring(lastSlash + 1);
                    BaseUrl = BaseUrl.Remove(lastSlash + 1);
                }
                _loadFullTask = new Lazy<Task>(() => LoadAndMergeFull(IsContext));
            }
        }

        public bool IsContext {get; private set;}
        public string ContextTargetID { get; private set; }
        private string _sort;
        public string Sort
        {
            get
            {
                return _sort;
            }
            set
            {
                _sort = value;
                RaisePropertyChanged("Sort");
            }
        }
        public string BaseUrl { get; private set; }
        public LinkViewModel Link { get; private set; }
		public DateTime? LastRefresh { get; set; }

        public ObservableCollection<ViewModelBase> FlatComments { get; private set; }

        private CommentShell LastChild(CommentShell shell)
        {
            if (shell.FirstChild != null)
            {
                CommentShell currentShell;
                if (_comments.TryGetValue(shell.FirstChild, out currentShell))
                {
                    return LastSibling(currentShell);
                }
            }
            return null;
        }
        private CommentShell LastSibling(CommentShell shell)
        {
            var currentShell = shell;
            while (currentShell.NextSibling != null)
            {
                //this is the end as far as we're concerned
                CommentShell tmpShell;
                if (!_comments.TryGetValue(currentShell.NextSibling, out tmpShell))
                {
                    return tmpShell;
                }
                else
                    currentShell = tmpShell;
            }
            return null;
        }

        private void MergeComments(CommentShell parent, IEnumerable<Thing> things, int depth = 0)
        {
            CommentShell priorSibling = parent != null ? LastChild(parent) : null;
			MoreViewModel priorMore = null;
            foreach (var child in things)
            {
				if(child.Data is More && ((More)child.Data).Children.Count > 0)
                {
					if(priorMore != null)
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
						if (!_knownUnloaded.ContainsKey(firstId))
						{
							priorMore = new MoreViewModel(this, parent != null ? parent.Id : null, firstId, ((More)child.Data).Children,((More)child.Data).Count, depth );
							_knownUnloaded.Add(firstId, priorMore);
						}
					}
                }
                else if (child.Data is Comment)
                {
					priorMore = null;
                    var commentId = ((Comment)child.Data).Id;

                    if (priorSibling == null && parent == null)
                    {
                        if (_firstChild != null)
                        {
                            priorSibling = LastSibling(_comments[_firstChild]);
                        }
                        else
                        {
                            _firstChild = commentId;
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

                    if (!_comments.ContainsKey(commentId))
                    {
                        priorSibling = MakeCommentShell(child.Data as Comment, depth, priorSibling);
                        _comments.Add(commentId, priorSibling);
                    }
                    else
                    {
                        var current = _comments[commentId];
                        current.PriorSibling = priorSibling != null ? priorSibling.Id : null;
                        current.NextSibling = null;
                        priorSibling = current;
                        MergeComment(priorSibling, child.Data as Comment);
                    }

                    var replies = ((Comment)child.Data).Replies;
                    if (replies != null)
                        MergeComments(priorSibling, replies.Data.Children, depth + 1);
                }
            }
        }

        private void MergeComment(CommentShell priorSibling, Comment thingData)
        {
            priorSibling.OriginType = CommentOriginType.Edited;
            priorSibling.InsertionWaveIndex = _commentOriginStack.Count - 1;
            priorSibling.Comment.Thing = thingData;
        }

        private CommentShell MakeCommentShell(Comment comment, int depth, CommentShell priorSibling)
        {
            var result = new CommentShell
            {
                Comment = new CommentViewModel(this, comment, comment.LinkId, depth),
				Id = comment.Id,
                Parent = comment.ParentId.StartsWith("t1_") ? comment.ParentId.Substring("t1_".Length) : null,
                PriorSibling = priorSibling != null ? priorSibling.Id : null,
                InsertionWaveIndex = _commentOriginStack.Count - 1,
                OriginType = _commentOriginStack.Last()
            };
            return result;
        }

        //this needs to be called on anything that comes back from a load more because reddit mostly just gives us a non structured listing of comments
        private void FixupParentage(Listing listing)
        {
            Dictionary<string, Comment> nameMap = new Dictionary<string, Comment>();
            foreach (var item in listing.Data.Children)
            {
				if (item.Data is Comment)
					nameMap.Add(((Comment)item.Data).Name, ((Comment)item.Data));
            }

            foreach (var item in new List<Thing>(listing.Data.Children))
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
                    listing.Data.Children.Remove(item);
                }
            }
        }

        private void InsertIntoFlatList(string firstChild, List<ViewModelBase> list)
        {
            CommentShell childShell;
            var targetChild = firstChild;
            while (targetChild != null)
            {
                if (_comments.TryGetValue(targetChild, out childShell))
                {
                    list.Add(childShell.Comment);
                    if (childShell.FirstChild != null)
                    {
                        InsertIntoFlatList(childShell.FirstChild, list);
                    }
                    targetChild = childShell.NextSibling;
                }
                else if(_knownUnloaded.ContainsKey(targetChild)) //if its not in the list check the known unloaded list (more)
                {
					list.Add(_knownUnloaded[targetChild]);
                    targetChild = null;
                }
                else //we must be looking at something missing because on context so we need to put out a 'loadfull' viewmodel
                {
                    list.Add(new LoadFullCommentsViewModel(this));
                    targetChild = null;
                }
            }
        }

        private void MergeDisplayReplacement(bool isFull, IEnumerable<ViewModelBase> replacements)
        {
            //need to find the above and do insertion of everything above
            //then insert the below
            //the middle needs to be merged and have the shells set to an edited 
            //origin type so we can display the differences caused by the load

            var replacementsList = replacements.ToList();

            //get rid of the load full sentinels since we're filling in the real versions, only if this is actually a context change
            if (isFull)
                while (FlatComments.Remove(_loadFullSentinel)) { }


            var firstExistingId = GetId(FlatComments.First());
            var lastExistingId = GetId(FlatComments.Last());


            var aboveComments = replacements.TakeWhile((vm) => GetId(vm) != firstExistingId).Reverse().ToList();

            List<ViewModelBase> mergableComments = new List<ViewModelBase>();
            foreach (var item in replacements.SkipWhile((vm) => GetId(vm) != firstExistingId))
            {
                mergableComments.Add(item);
                if (GetId(item) == lastExistingId)
                    break;
            }

            var belowComments = replacements.SkipWhile((vm) => GetId(vm) != lastExistingId).Skip(1).ToList();

            

            if (mergableComments.Count != FlatComments.Count) //otherwise nothing to do, just add in the above and below
            {

				while (mergableComments.Count < FlatComments.Count && FlatComments.Count > 0)
					FlatComments.RemoveAt(FlatComments.Count - 1);
                for (int flatI = 0, mergableI = 0; flatI < FlatComments.Count && mergableI < mergableComments.Count; flatI++, mergableI++)
                {
                    if (FlatComments[flatI] != mergableComments[mergableI])
                    {
                        //need to skip ahead one on flatI since we just added another one and we dont want that to be the new comparison point
                        FlatComments.Insert(flatI++, mergableComments[mergableI]);
                    }
                }
            } 

            foreach (var comment in aboveComments)
            {
                FlatComments.Insert(0, comment);
            }

            foreach (var comment in belowComments)
            {
                FlatComments.Add(comment);
            }
        }

        private string GetId(ViewModelBase viewModel)
        {
            if (viewModel is CommentViewModel)
                return ((CommentViewModel)viewModel).Id;
            else if (viewModel is MoreViewModel)
                return ((MoreViewModel)viewModel).Id;
            else
                return "";
        }

        private void MergeDisplayChildren(IEnumerable<ViewModelBase> newChildren, IEnumerable<string> replaceId)
        {
            //the children will be in a contiguous block so we just need to find the existing viewmodel that 
            //matches the afterId then we can add each one in series
			bool foundFirst = false;
			for (int i = 0; i < FlatComments.Count; i++)
            {
                var commentId = GetId(FlatComments[i]);
                if (replaceId.Contains(commentId))
                {
					FlatComments.RemoveAt(i);
					if (!foundFirst)
					{
						foreach (var child in newChildren)
						{
							if ((FlatComments.Count - 1) <= i)
								FlatComments.Add(child);
							else
								FlatComments.Insert(i, child);

							i++;
						}
					}
					foundFirst = true; ;
                }
            }
        }

        public async Task LoadMore(MoreViewModel target)
        {
            List<ViewModelBase> flatChilden = new List<ViewModelBase>();
            string moreId = null;
            await SnooStreamViewModel.NotificationService.Report("loading more comments", async () =>
                {
					var listing = await SnooStreamViewModel.RedditService.GetMoreOnListing(new More { Children = target.Ids, ParentId = target.ParentId, Count = target.Count }, Link.Link.Id, Link.Link.Subreddit);
                    lock(this)
                    {
                        FixupParentage(listing);
                        var firstChild = listing.Data.Children.FirstOrDefault(thing => thing.Data is Comment);
                        if(firstChild == null)
                            return;

                        var parentId = ((Comment)firstChild.Data).ParentId;
                        CommentShell parentShell;
                        if (!_comments.TryGetValue(parentId.Replace("t1_", ""), out parentShell))
                        {
                            parentShell = null;
                        }

                        MergeComments(parentShell, listing.Data.Children, parentShell == null ? 0 : parentShell.Comment.Depth + 1);
                        moreId = ((Comment)firstChild.Data).Id;
                        InsertIntoFlatList(moreId, flatChilden);
						foreach (var child in flatChilden)
						{
							_knownUnloaded.Remove(GetId(child));
						}
                    }
                });

			if (moreId != null)
				MergeDisplayChildren(flatChilden, target.Ids);
			else
				FlatComments.Remove(target);
        }

        Lazy<Task> _loadFullTask;

        public Task LoadFull()
        {
            return _loadFullTask.Value;
        }

        public async Task Refresh(bool onlyNew)
        {
            await LoadAndMergeFull(IsContext);
        }

		public void HideDecendents(string id)
		{
			foreach (var vmb in Decendents(id))
			{
				FlatComments.Remove(vmb);
			}
		}

		public void ShowDecendents(CommentViewModel parent)
		{
			var insertionPoint = FlatComments.IndexOf(parent) + 1;
			foreach (var vmb in Decendents(parent.Id).Reverse())
			{
				FlatComments.Insert(insertionPoint, vmb);
			}
		}

		public IEnumerable<ViewModelBase> Decendents(string id)
		{
			var result = new List<ViewModelBase>();

			var currentShell = _comments[id];
			if (currentShell != null && currentShell.FirstChild != null)
			{
				CommentShell tmpShell;
				if (!_comments.TryGetValue(currentShell.FirstChild, out tmpShell))
				{
					MoreViewModel moreVm;
					if (_knownUnloaded.TryGetValue(currentShell.FirstChild, out moreVm))
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
				result.AddRange(Decendents(currentShell.Id));


			while (currentShell.NextSibling != null)
			{
				
				CommentShell tmpShell;
				if (!_comments.TryGetValue(currentShell.NextSibling, out tmpShell))
				{
					//this is the end as far as we're concerned
					MoreViewModel moreValues;
					if (_knownUnloaded.TryGetValue(currentShell.NextSibling, out moreValues))
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
						result.AddRange(Decendents(currentShell.Id));
				}
			}
			return result;
		}

        public async Task<List<ViewModelBase>> LoadImpl(bool isContext)
        {
            List<ViewModelBase> flatChildren = new List<ViewModelBase>();
			await SnooStreamViewModel.NotificationService.Report("loading comments", async () =>
			{
                string subreddit = null;
                if (Link == null)
                {
                    var splitUrl = BaseUrl.Split('/');
                    subreddit = splitUrl[Array.IndexOf(splitUrl, "r") + 1];
                }
                else
                    subreddit = Link.Link.Subreddit;

                var listing = await SnooStreamViewModel.RedditService.GetCommentsOnPost(subreddit, (isContext) ? BaseUrl + ContextTargetID : BaseUrl, null, context: isContext ? "3" : null, sort: Sort);
				lock(this)
				{
					var firstChild = listing.Data.Children.FirstOrDefault(thing => thing.Data is Comment);
					if(firstChild == null)
						return;

					_commentOriginStack.Add(CommentOriginType.New);
					MergeComments(null, listing.Data.Children, 0);
					InsertIntoFlatList(((Comment)firstChild.Data).Id, flatChildren);
				}
			});
            return flatChildren;
        }

        public async Task<List<ViewModelBase>> LoadStoredImpl(bool isContext)
        {
            List<ViewModelBase> flatChildren = new List<ViewModelBase>();
            await SnooStreamViewModel.NotificationService.Report("loading stored comments", async () =>
            {
                var things = await SnooStreamViewModel.OfflineService.RetrieveOrderedThings("comments:" + BaseUrl + "?context=" + ContextTargetID + "&sort=" + Sort, TimeSpan.FromDays(1024));
                lock (this)
                {
                    var firstChild = things.FirstOrDefault(thing => thing.Data is Comment);
                    if (firstChild == null)
                        return;
                    _commentOriginStack.Add(CommentOriginType.New);
                    MergeComments(null, things, 0);
                    InsertIntoFlatList(((Comment)firstChild.Data).Id, flatChildren);
                }
            });
            return flatChildren;
        }

		public Listing DumpListing()
		{
            if (_firstChild != null)
            {
                return DumpListing(_firstChild);
            }
            else
            {
                return new Listing { Data = new ListingData { Children = new List<Thing>() } };
            }
			
		}

		public void LoadDump(Listing comments)
		{
			List<ViewModelBase> flatChildren = new List<ViewModelBase>();
			var firstChild = comments.Data.Children.FirstOrDefault(thing => thing.Data is Comment);
			if (firstChild == null)
				return;
			_commentOriginStack.Add(CommentOriginType.New);
			MergeComments(null, comments.Data.Children, 0);
			InsertIntoFlatList(((Comment)firstChild.Data).Id, flatChildren);
			FlatComments = new ObservableCollection<ViewModelBase>(flatChildren);
		}

        private Listing DumpListing(string firstChild)
        {
            var result = new Listing { Kind = "listing", Data = new ListingData { Children = new List<Thing>() } };
			CommentShell currentShell;
			currentShell = DumpItem(firstChild, result);


			while (currentShell != null && currentShell.NextSibling != null)
            {
				currentShell = DumpItem(currentShell.NextSibling, result);
            }
            return result;
        }

		private CommentShell DumpItem(string itemId, Listing result)
		{
			CommentShell currentShell;
			if (!_comments.TryGetValue(itemId, out currentShell))
			{
				MoreViewModel moreValues;
				if (_knownUnloaded.TryGetValue(itemId, out moreValues))
				{
					result.Data.Children.Add(new Thing { Kind = "more", Data = new More { Children = moreValues.Ids, ParentId = moreValues.ParentId, Count = moreValues.Count } });
				}
				return null;
			}
			else
			{
				var firstThing = new Thing { Kind = "t1", Data = currentShell.Comment.Thing };
				result.Data.Children.Add(firstThing);

				if (currentShell.FirstChild != null)
					((Comment)firstThing.Data).Replies = DumpListing(currentShell.FirstChild);
			}
			return currentShell;
		}

        public async Task StoreCurrent()
        {
            if(_firstChild != null)
            {
                var rootListing = DumpListing(_firstChild);
                await SnooStreamViewModel.OfflineService.StoreOrderedThings("comments:" + BaseUrl + "?context=" + ContextTargetID + "&sort=" + Sort, rootListing.Data.Children);
            }
        }

        public async Task LoadAndMergeFull(bool isContext)
        {
			_commentsContentStream = new Lazy<CommentsContentStreamViewModel>(() => new CommentsContentStreamViewModel(this));
			LastRefresh = DateTime.Now;
            var flatChildren = await LoadImpl(isContext);

            if (flatChildren.Count > 0)
            {
                if (FlatComments.Count == 0)
                {
                    if (isContext)
                        FlatComments.Add(_loadFullSentinel);

                    foreach (var comment in flatChildren)
                    {
                        FlatComments.Add(comment);
                    }
                }
                else
                {
                    MergeDisplayReplacement(true, flatChildren);
                }
            }
        }

		public async void SetSort(string sort)
		{
			Sort = sort;
			await Refresh(false);
		}

		public async Task MaybeRefresh()
		{
			if (LastRefresh == null || (DateTime.Now - LastRefresh.Value).TotalMinutes > 30)
			{
				await Refresh(false);
			}
		}

        public RelayCommand GotoLink
        {
            get 
            {
                return new RelayCommand(() => SnooStreamViewModel.CommandDispatcher.GotoLink(_context, Link.Link.Url));
            }
        }

        public RelayCommand GotoReply
        {
            get
            {
                return new RelayCommand(() => SnooStreamViewModel.CommandDispatcher.GotoReplyToPost(_context, this));
            }
        }

        public RelayCommand GotoEditPost
        {
            get
            {
                return new RelayCommand(() => SnooStreamViewModel.CommandDispatcher.GotoEditPost(_context, Link));
            }
        }

        public RelayCommand  RefreshCommand
        {
            get
            {
                return new RelayCommand(() => Refresh(false));
            }
        }

        public RelayCommand<string> SetSortCommand
        {
            get
            {
                return new RelayCommand<string>((str) => SetSort(str));
            }
        }

        public RelayCommand FindInCommentsCommand
        {
            get
            {
                return new RelayCommand(() => { throw new NotImplementedException(); });
            }
        }

        ViewModelBase _currentlyFocused;
        public ViewModelBase CurrentlyFocused
        {
            get
            {
                return _currentlyFocused;
            }
            set
            {
                if(_currentlyFocused != value)
                {
                    if (FocusChanged != null)
                        FocusChanged(_currentlyFocused, value);

                    _currentlyFocused = value;
                }
            }
        }


        public event Action<ViewModelBase, ViewModelBase> FocusChanged;
    } 
}