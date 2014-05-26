using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommonImageAquisition.SimpleAPI
{
	class Memefive : IAquisitionAPI
	{
		private static Regex hashRe = new Regex(@"^http:\/\/(?:www\.)?(?:memefive\.com)\/meme\/([\w]+)\/?");
		private static Regex altHashRe = new Regex(@"^http:\/\/(?:www\.)?(?:memefive\.com)\/([\w]+)\/?");

		public string GetImageFromUri(Uri uri)
		{
			var href = uri.OriginalString;
			var groups = hashRe.Match(href).Groups;

			if (groups == null)
			{
				groups = altHashRe.Match(href).Groups;
			}
			if (groups != null)
			{
				return string.Format("http://memefive.com/memes/{0}.jpg", groups[1].Value);
			}
			else
				return null;
		}
	}
}
