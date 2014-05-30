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
