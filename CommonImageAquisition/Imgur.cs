using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommonImageAquisition
{
    class Imgur
    {
        //Transliterated from Reddit Enhancement Suite https://github.com/honestbleeps/Reddit-Enhancement-Suite/blob/master/lib/reddit_enhancement_suite.user.js
        private static Regex hashRe = new Regex(@"^https?:\/\/(?:i\.|m\.|edge\.|www\.)*imgur\.com\/(?!gallery)(?!removalrequest)(?!random)(?!memegen)([A-Za-z0-9]{5}|[A-Za-z0-9]{7})[sbtmlh]?(\.(?:jpe?g|gif|png))?(\?.*)?$");
        private static Regex albumHashRe = new Regex(@"https?:\/\/(?:i\.|m\.)?imgur\.com\/(?:a|gallery)\/([\w]+)(\..+)?(?:\/)?(?:#\w*)?$");
		private static string apiPrefix = "https://api.imgur.com/2/";

        internal static bool IsAPI(Uri uri)
        {
            var href = uri.OriginalString;

			if (href.Contains("/gallery/"))
				return true;

            var groups = hashRe.Match(href).Groups;
            GroupCollection albumGroups = null;

            if (groups.Count == 0 || (groups.Count > 0 && string.IsNullOrWhiteSpace(groups[0].Value)))
                albumGroups = albumHashRe.Match(href).Groups;

            return ((albumGroups != null && albumGroups.Count > 2 && string.IsNullOrWhiteSpace(albumGroups[2].Value)) ||
                groups.Count > 1 && !string.IsNullOrWhiteSpace(groups[1].Value));
            
        }

        internal static async Task<IEnumerable<Tuple<string, string>>> GetImagesFromUri(string title, Uri uri)
        {
            var href = uri.OriginalString;
            var groups = hashRe.Match(href).Groups;
            GroupCollection albumGroups = null;

            if (groups.Count == 0 || (groups.Count > 0 && string.IsNullOrWhiteSpace(groups[0].Value)))
                albumGroups = albumHashRe.Match(href).Groups;

            if (groups.Count > 2 && string.IsNullOrWhiteSpace(groups[2].Value))
            {
                if (Regex.IsMatch(groups[1].Value, "[&,]"))
                {
                    var hashes = Regex.Split(groups[1].Value, "[&,]");
                    //Imgur doesn't really care about the extension and the browsers don't seem to either.
                    return hashes
                        .Select(hash => Tuple.Create(title, string.Format("http://i.imgur.com/{0}.gif", hash)));

                }
                else
                {
                    if (uri.AbsolutePath.ToLower().StartsWith("/gallery"))
                    {
                        return await GetImagesFromUri(title, new Uri("http://imgur.com/a/" + groups[1].Value));
                    }
                    else
                    {
                        //Imgur doesn't really care about the extension and the browsers don't seem to either.
                        return new Tuple<string, string>[] { Tuple.Create(title, string.Format("http://i.imgur.com/{0}.gif", groups[1].Value)) };
                    }
                }
            }
            else if (albumGroups.Count > 2 && string.IsNullOrWhiteSpace(albumGroups[2].Value))
            {
                var apiURL = string.Format("{0}album/{1}.json", apiPrefix, albumGroups[1].Value);
                var jsonResult = await HttpClientUtility.Get(apiURL, true);
                if(string.IsNullOrWhiteSpace(jsonResult))
                    return Enumerable.Empty<Tuple<string, string>>();

				JObject result = null;
				try
				{
					result = JsonConvert.DeserializeObject(jsonResult) as JObject;
				}
				catch { }
				
                if (result != null && result.HasValues)
                {
                    JToken errorToken;
                    if (result.TryGetValue("error", out errorToken))
                    {
                        if(((JObject)errorToken)["message"].Value<string>() == "Album not found")
                            return new Tuple<string, string>[] { Tuple.Create(title, string.Format("http://i.imgur.com/{0}.gif", albumGroups[1].Value)) };
                        else
                            return Enumerable.Empty<Tuple<string, string>>();
                    }

                    var albumTitleElement = (string)((JObject)result.GetValue("album")).GetValue("title");
                    var albumTitle = string.IsNullOrWhiteSpace(albumTitleElement) ? title : albumTitleElement;

                    var resultImages = ((IEnumerable)((JObject)result.GetValue("album")).GetValue("images"))
                        .Cast<JObject>()
                        .Select(e => 
                            {
                                var caption = (string)((JObject)e.GetValue("image")).GetValue("caption");

                                if (!string.IsNullOrWhiteSpace(caption))
                                    caption = caption.Replace("&#039;", "'").Replace("&#038;", "&").Replace("&#034;", "\"");

                                return Tuple.Create(string.IsNullOrWhiteSpace(caption) ? albumTitle : caption, (string)((JObject)e.GetValue("links")).GetValue("original"));
                            })
						.ToArray();
					return resultImages;
                }
                else
					return new Tuple<string, string>[] { Tuple.Create(title, string.Format("http://i.imgur.com/{0}.gif", albumGroups[1].Value)) };
            }
            else
                return Enumerable.Empty<Tuple<string, string>>();
        }
    }
}
