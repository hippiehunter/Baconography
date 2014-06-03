using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommonImageAcquisition.SimpleAPI
{
    class Livememe : IAcquisitionAPI
    {
        //Transliterated from Reddit Enhancement Suite https://github.com/honestbleeps/Reddit-Enhancement-Suite/blob/master/lib/reddit_enhancement_suite.user.js
        private Regex hashRe = new Regex(@"^http:\/\/(?:www\.livememe\.com|lvme\.me)\/(?!edit)([\w]+)\/?");

        public string GetImageFromUri(Uri uri)
        {
            var href = uri.OriginalString;
            var groups = hashRe.Match(href).Groups;

			if (groups.Count > 0 && !string.IsNullOrWhiteSpace(groups[1].Value))
			{
				return string.Format("http://www.livememe.com/{0}.jpg", groups[1].Value);
			}
			else
				return null;
        }
    }
}
