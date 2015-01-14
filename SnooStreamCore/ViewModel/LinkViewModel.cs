using CommonResourceAcquisition.ImageAcquisition;
using CommonResourceAcquisition.VideoAcquisition;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SnooSharp;
using SnooStream.Common;
using SnooStream.Model;
using SnooStream.Services;
using SnooStream.ViewModel.Content;
using SnooStream.ViewModel.Popups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class LinkViewModel : ViewModelBase, ILinkViewModel
    {
        public LinkViewModel(ViewModelBase context, Link link)
        {
            Context = context;
            Link = link;
            Comments = new CommentsViewModel(this, link);
            _content = new Lazy<ContentViewModel>(() => SnooStream.ViewModel.Content.ContentViewModel.MakeContentViewModel(link.Url, link.Title, this, link.Thumbnail));

            var commonCommands = new List<CommandViewModel.CommandItem>
                {
                    new CommandViewModel.CommandItem
                    {
                        DisplayText = "Share Link",
                        Command = Share
                    },
                    new CommandViewModel.CommandItem
                    {
                        DisplayText = "Report",
                        Command = Report
                    },
                    new CommandViewModel.CommandItem
                    {
                        DisplayText = "Save",
                        Command = Save
                    },
                    new CommandViewModel.CommandItem
                    {
                        DisplayText = "User Details",
                        Command = GotoUserDetails
                    }
                };

            LinkMore = new CommandViewModel
                {
                    Commands = new List<CommandViewModel.CommandItem>(commonCommands) {  new CommandViewModel.CommandItem
                    {
                        DisplayText = "Hide",
                        Command = Hide
                    } }
                };
            CommentMore = new CommandViewModel
                {
                    Commands = new List<CommandViewModel.CommandItem>(commonCommands)
                };
        }

        public bool HasContext { get { return true; } }

        Lazy<ContentViewModel> _content;
        public ContentViewModel Content
        {
            get
            {
                return _content.Value;
            }
        }

        public ViewModelBase Context { get; private set; }
        public CommentsViewModel Comments { get; internal set; }
        public Link Link { get; private set; }
        public int CommentsLastViewed { get; private set; }
        //need to add load helpers here for kicking off preview loads when we get near things

        internal void MergeLink(Link link)
        {
            Votable.MergeVotable(link);
            Link.CommentCount = link.CommentCount;
            RaisePropertyChanged("CommentCount");
        }

        public MarkdownData SelfText
        {
            get
            {
                return SnooStreamViewModel.MarkdownProcessor.Process(Link.Selftext);
            }
        }
        public bool FromMultiReddit { get; set; }

        string _domain = null;
        public string Domain
        {
            get
            {
                if (_domain == null)
                {
                    _domain = new Uri(Link.Url).Authority;
                    if (_domain == "reddit.com" && Link.Url.ToLower().Contains(Subreddit.ToLower()))
                        _domain = "self." + Subreddit.ToLower();
                }
                return _domain;
            }
        }

        //this should show only moderator info
        public AuthorFlairKind AuthorFlair
        {
            get
            {
                return AuthorFlairKind.None;
            }
        }

        public string AuthorFlairText { get; set; }

        public bool HasAuthorFlair
        {
            get
            {
                return (!String.IsNullOrWhiteSpace(AuthorFlairText));
            }
        }

        public string Author
        {
            get
            {
                return Link.Author;
            }
        }

        public string Subreddit
        {
            get
            {
                return Link.Subreddit;
            }
        }

        public string Title
        {
            get
            {
                return Link.Title.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'").Trim();
            }
        }

        public int CommentCount
        {
            get
            {
                return Link.CommentCount;
            }
        }

        public bool IsSelfPost
        {
            get
            {
                return Link.IsSelf;
            }
        }

        public string Url
        {
            get
            {
                return Link.Url;
            }
        }

        VotableViewModel _votable;
        public VotableViewModel Votable
        {
            get
            {
                if (_votable == null)
                    _votable = new VotableViewModel(Link, () => RaisePropertyChanged("Votable"));
                return _votable;
            }
        }

        public bool HasThumbnail
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Thumbnail) && Thumbnail != "self" && Thumbnail != "nsfw" && Thumbnail != "default";
            }
        }

        public string Thumbnail
        {
            get
            {
                return Link.Thumbnail;
            }
        }


        public DateTime CreatedUTC
        {
            get
            {
                return Link.CreatedUTC;
            }
        }

        public LinkMeta Metadata { get; private set; }

        internal void UpdateMetadata(LinkMeta linkMeta)
        {
            Metadata = linkMeta;
            RaisePropertyChanged("Metadata");
        }

        public RelayCommand GotoComments
        {
            get
            {
                return new RelayCommand(() =>
                {
                    Comments.LoadFull();
                    SnooStreamViewModel.NavigationService.NavigateToComments(Comments);
                });
            }
        }

        private CommandViewModel LinkMore;
        private CommandViewModel CommentMore;

        public RelayCommand<object> ShowMoreLink { get { return new RelayCommand<object>((obj) => SnooStreamViewModel.NavigationService.ShowPopup(LinkMore, obj)); } }
        public RelayCommand<object> ShowMoreComment { get { return new RelayCommand<object>((obj) => SnooStreamViewModel.NavigationService.ShowPopup(CommentMore, obj)); } }
        public RelayCommand Share { get { return new RelayCommand(() => SnooStreamViewModel.SystemServices.ShareLink(Url, Title, "posted by " + Author + " to " + Subreddit)); } }
        public RelayCommand GotoWeb { get { return new RelayCommand(() => SnooStreamViewModel.NavigationService.NavigateToWeb(Link.Url)); } }
        public RelayCommand GotoLink { get { return new RelayCommand(() => SnooStreamViewModel.CommandDispatcher.GotoLink(this, Link.Url)); } }
        public RelayCommand GotoSubreddit { get { return new RelayCommand(() => SnooStreamViewModel.CommandDispatcher.GotoSubreddit(Subreddit)); } }
        public RelayCommand GotoUserDetails { get { return new RelayCommand(() => SnooStreamViewModel.CommandDispatcher.GotoUserDetails(Author)); } }
        public RelayCommand Report { get { return new RelayCommand(() => SnooStreamViewModel.RedditService.AddReportOnThing(Link.Name)); } }
        public RelayCommand Hide
        {
            get
            {
                return new RelayCommand(async () =>
                    {
                        var linkRiverContext = Context as LinkRiverViewModel;
                        if (linkRiverContext != null)
                            linkRiverContext.Links.Remove(this);

                        await SnooStreamViewModel.RedditService.HideThing(Link.Name);
                    });
            }
        }
        public RelayCommand Save { get { return new RelayCommand(() => SnooStreamViewModel.RedditService.AddSavedThing(Link.Name)); } }
        public string Id
        {
            get { return Link.Id; }
        }
    }
}
