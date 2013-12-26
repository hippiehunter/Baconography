﻿using CommonImageAquisition;
using CommonVideoAquisition;
using GalaSoft.MvvmLight;
using SnooSharp;
using SnooStream.Common;
using SnooStream.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class LinkViewModel : ViewModelBase
    {
        public LinkViewModel(ViewModelBase context, Link link)
        {
            Context = context;
            Link = link;
            _content = new Lazy<Task<ContentViewModel>>(LoadContent);

        }

        private async Task<ContentViewModel> LoadContent()
        {
            try
            {
                string targetHost = null;
                string fileName = null;

                if(Uri.IsWellFormedUriString(Link.Url, UriKind.RelativeOrAbsolute))
                {
                    var uri = new Uri(Link.Url);
                    targetHost = uri.DnsSafeHost.ToLower();
                    fileName = uri.AbsolutePath;
                }


                if(Link.IsSelf)
                    return new SelfContentViewModel(this, Link);
                else if(LinkGlyphUtility.IsComment(Link.Url) ||
                    LinkGlyphUtility.IsCommentsPage(Link.Url) ||
                    LinkGlyphUtility.IsSubreddit(Link.Url) ||
                    LinkGlyphUtility.IsUser(Link.Url) ||
                    LinkGlyphUtility.IsUserMultiReddit(Link.Url))
                {
                    return new InternalRedditContentViewModel(this, Link.Url);
                }
                else if (targetHost == "www.youtube.com" ||
                    targetHost == "www.youtu.be" ||
                    targetHost == "youtu.be" ||
                    targetHost == "youtube.com" ||
                    targetHost == "vimeo.com" ||
                    targetHost == "www.vimeo.com" ||
                    targetHost == "liveleak.com" ||
                    targetHost == "www.liveleak.com")
                {
                    if (VideoAquisition.IsAPI(Link.Url))
                    {
                        return new VideoViewModel(this, Link.Url);
                    }
                    else
                    {
                        return new WebViewModel(this, true, Link.Url);
                    }
                }
                else if (targetHost == "www.imgur.com" ||
                    targetHost == "imgur.com" ||
                    targetHost == "i.imgur.com" ||
                    targetHost == "min.us" ||
                    targetHost == "www.quickmeme.com" ||
                    targetHost == "www.livememe.com" ||
                    targetHost == "livememe.com" ||
                    targetHost == "i.qkme.me" ||
                    targetHost == "quickmeme.com" ||
                    targetHost == "qkme.me" ||
                    targetHost == "memecrunch.com" ||
                    targetHost == "flickr.com" ||
                    targetHost == "www.flickr.com" ||
                    fileName.EndsWith(".jpg") ||
                    fileName.EndsWith(".gif") ||
                    fileName.EndsWith(".png") ||
                    fileName.EndsWith(".jpeg"))
                {
                    if (ImageAquisition.IsImageAPI(Link.Url))
                    {
                        var imageApiResults = await ImageAquisition.GetImagesFromUrl(Link.Title, Link.Url);
                        if (imageApiResults != null && imageApiResults.Count() > 1)
                            return new AlbumViewModel(this, Link.Url, imageApiResults, Link.Title);
                        else if (imageApiResults != null)
                            return new ImageViewModel(this, imageApiResults.First().Item2, imageApiResults.First().Item1, null);
                    }
                    return new WebViewModel(this, true, Link.Url);
                }
                else
                    return new WebViewModel(this, false, Link.Url);
            }
            catch (Exception ex)
            {
                return new ErrorContentViewModel(this, ex);
            }
        }

        public ViewModelBase Context { get; private set; }
        private Lazy<Task<ContentViewModel>> _content;
        public ContentViewModel Content
        {
            get
            {
                if (_content.Value.IsCompleted)
                    return _content.Value.Result;
                else
                    return null;
            }
        }
        public CommentsViewModel Comments { get; private set; }
        public Link Link { get; private set; }
        public int CommentsLastViewed { get; private set; }
        //need to add load helpers here for kicking off preview loads when we get near things

        internal void MergeLink(Link link)
        {
            throw new NotImplementedException();
        }

        object _selfText;
        public object SelfText
        {
            get
            {
                if (_selfText == null)
                {
                    _selfText = SnooStreamViewModel.MarkdownProcessor.Process(Link.Selftext);
                }
                return _selfText;
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
    }
}
