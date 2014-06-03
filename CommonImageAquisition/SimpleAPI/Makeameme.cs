using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommonImageAcquisition.SimpleAPI
{
	class Makeameme : IAcquisitionAPI
	{
		private static Regex hashRe = new Regex(@"^http:\/\/makeameme\.org\/meme\/([\w-]+)\/?");

		public string GetImageFromUri(Uri uri)
		{
			var href = uri.OriginalString;
			var groups = hashRe.Match(href).Groups;

			if (groups != null)
				return string.Format("http://makeameme.org/media/created/{0}.jpg", groups[1].Value);

			else
				return null;
		}
	}
}
