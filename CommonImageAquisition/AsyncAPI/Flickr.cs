using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommonImageAquisition.AsyncAPI
{
    class Flickr : IAsyncAquisitionAPI
    {
        //Transliterated from Reddit Enhancement Suite https://github.com/honestbleeps/Reddit-Enhancement-Suite/blob/master/lib/reddit_enhancement_suite.user.js
        private Regex hashRe = new Regex(@"^http:\/\/(?:\w+)\.?flickr\.com\/(?:.*)\/([\d]{10})\/?(?:.*)?$");

		public bool IsMatch(Uri uri)
        {
            return hashRe.IsMatch(uri.OriginalString);
        }

        struct OEmbedResult
        {
            public string version;
	        public string type;
	        public int width;
	        public int height;
	        public string title;
	        public string url;
	        public string author_name;
	        public string author_url;
	        public string provider_name;
            public string provider_url;
        }

		public async Task<IEnumerable<Tuple<string, string>>> GetImagesFromUri(string title, Uri uri)
        {
            try
            {
                var href = uri.OriginalString;
                if (href.IndexOf("/sizes") == -1)
                {
                    var inPosition = href.IndexOf("/in/");
                    var inFragment = "";
                    if (inPosition != -1)
                    {
                        inFragment = href.Substring(inPosition);
                        href = href.Substring(0, inPosition);
                    }

                    href += "/sizes/c" + inFragment;
                }
                href = href.Replace("/lightbox", "");
                var jsonResult = await HttpClientUtility.Get("http://www.flickr.com/services/oembed/?format=json&url=" + Uri.EscapeUriString(href));
                var resultObject = JsonConvert.DeserializeObject<OEmbedResult>(jsonResult);
                return new Tuple<string, string>[] { Tuple.Create(resultObject.author_name + " via " + resultObject.provider_name + " : " + resultObject.title, resultObject.url) };
            }
            catch
            {
                return Enumerable.Empty<Tuple<string, string>>();
            }
        }
	}
}
