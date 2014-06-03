using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommonImageAcquisition.SimpleAPI
{
    class Picsarus : IAcquisitionAPI
    {
        //Transliterated from Reddit Enhancement Suite https://github.com/honestbleeps/Reddit-Enhancement-Suite/blob/master/lib/reddit_enhancement_suite.user.js
        private static Regex hashRe = new Regex(@"^https?:\/\/(?:[i.]|[edge.]|[www.])*picsarus.com\/(?:r\/[\w]+\/)?([\w]{6,})(\..+)?$");

        public string GetImageFromUri(Uri uri)
        {
            var href = uri.OriginalString;
            var groups = hashRe.Match(href).Groups;

			if (groups != null)
				return string.Format("http://www.picsarus.com/{0}.jpg", groups[1].Value);

			else
				return null;
        }

    }
}
