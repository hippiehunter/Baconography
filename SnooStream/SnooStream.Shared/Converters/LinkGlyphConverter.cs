using SnooSharp;
using SnooStream.Model;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace SnooStream.Converters
{
    public class LinkGlyphConverter : IValueConverter
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

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return GetLinkGlyph(value);
        }



        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
