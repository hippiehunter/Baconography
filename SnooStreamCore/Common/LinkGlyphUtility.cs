using SnooSharp;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SnooStream.Model
{
    public class LinkGlyphUtility
    {
        public const string NavRightGlyph = "\uE0AD";
        public const string PhotoGlyph = "\uE114";
        public const string VideoGlyph = "\uE116";
        public const string WebGlyph = "\uE128";
        public const string DetailsGlyph = "\uE14C";
        public const string MultiredditGlyph = "\uE17D";
        public const string UserGlyph = "\uE136";
        public const string CommentGlyph = "\uE14C";

		public static string GetLinkGlyph(Object value)
		{
			try
			{
				string subreddit = "";
				string targetHost = "";
				string filename = "";
				Uri uri = null;

				if (value is LinkViewModel)
				{
					var linkViewModel = value as LinkViewModel;

					if (linkViewModel.Link.IsSelf)
						return DetailsGlyph;

					uri = new Uri(linkViewModel.Link.Url);
					filename = uri.AbsolutePath;
					targetHost = uri.DnsSafeHost.ToLower();
					subreddit = linkViewModel.Link.Subreddit;
				}
				else if (value is Link)
				{
					var link = value as Link;

					if (link.IsSelf)
						return DetailsGlyph;

					uri = new Uri(link.Url);
					filename = uri.AbsolutePath;
					targetHost = uri.DnsSafeHost.ToLower();
					subreddit = link.Subreddit;
				}
				else if (value is string)
				{
					uri = new Uri(value as string);
					filename = uri.AbsolutePath;
					targetHost = uri.DnsSafeHost.ToLower();
				}

				if (subreddit == "videos" ||
					targetHost == "www.youtube.com" ||
					targetHost == "www.youtu.be" ||
					targetHost == "youtu.be" ||
					targetHost == "youtube.com" ||
					targetHost == "vimeo.com" ||
					targetHost == "www.vimeo.com" ||
					targetHost == "liveleak.com" ||
					targetHost == "www.liveleak.com")
					return VideoGlyph;

				if (targetHost == "www.imgur.com" ||
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
					filename.EndsWith(".jpg") ||
					filename.EndsWith(".gif") ||
					filename.EndsWith(".png") ||
					filename.EndsWith(".jpeg"))
					return PhotoGlyph;

				if (uri != null)
				{

					if (LinkGlyphUtility.UserMultiredditRegex.IsMatch(uri.AbsoluteUri) || LinkGlyphUtility.SubredditRegex.IsMatch(uri.AbsoluteUri))
						return MultiredditGlyph;
					else if (LinkGlyphUtility.UserRegex.IsMatch(uri.AbsoluteUri))
						return UserGlyph;
					else if (LinkGlyphUtility.CommentRegex.IsMatch(uri.AbsoluteUri) || LinkGlyphUtility.CommentsPageRegex.IsMatch(uri.AbsoluteUri))
						return CommentGlyph;
				}

			}
			catch { }
			return WebGlyph;
		}

        //Subreddit:
        public static Regex SubredditRegex = new Regex("(?:^|\\s|reddit.com)/r/[a-zA-Z0-9_.]+/?$");

        //Comments page:
        public static Regex CommentsPageRegex = new Regex("(?:^|\\s|reddit.com)/r/[a-zA-Z0-9_.]+/comments/[a-zA-Z0-9_]+/(?:[a-zA-Z0-9_]+/)*?");

        //Short URL comments page:
        public static Regex ShortCommentsPageRegex = new Regex("(?:redd.it)/[a-zA-Z0-9_.]+/?");

        //Comment:
        public static Regex CommentRegex = new Regex("(?:^|\\s|reddit.com)/r/[a-zA-Z0-9_.]+/comments/[a-zA-Z0-9_]+/[a-zA-Z0-9_]+/[a-zA-Z0-9_]+/?");

        //User Multireddit:
        public static Regex UserMultiredditRegex = new Regex("(?:^|\\s|reddit.com)/u(?:ser)*/[a-zA-Z0-9_./-]+/m/[a-zA-Z0-9_]+/?$");

        //User:
        public static Regex UserRegex = new Regex("(?:^|\\s|reddit.com)/u(?:ser)*/[a-zA-Z0-9_/-]+/?$");


        public static bool IsSubreddit(string url)
        {
            return SubredditRegex.IsMatch(url);
        }

        public static bool IsCommentsPage(string url)
        {
            return CommentsPageRegex.IsMatch(url);
        }

        public static bool IsComment(string url)
        {
            return CommentRegex.IsMatch(url);
        }

        public static bool IsUserMultiReddit(string url)
        {
            return UserMultiredditRegex.IsMatch(url);
        }

        public static bool IsUser(string url)
        {
            return UserRegex.IsMatch(url);
        }
    }
}
