using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommonImageAcquisition.SimpleAPI
{
    class Memecrunch : IAcquisitionAPI
    {
        private static Regex hashRe = new Regex(@"^http:\/\/memecrunch.com\/meme\/([0-9A-Z]+)\/([\w\-]+)(\/image\.(png|jpg))?");

        public string GetImageFromUri(Uri uri)
        {
            var href = uri.OriginalString;
            var groups = hashRe.Match(href).Groups;

			if (groups != null)
				return string.Format("http://memecrunch.com/meme/{0}/{1}/image.png", groups[1].Value, groups[2].Value ?? "null");

			else
				return null;
        }
    }
}
