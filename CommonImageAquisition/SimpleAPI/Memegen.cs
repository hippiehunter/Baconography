using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommonImageAquisition.SimpleAPI
{
	class Memegen : IAquisitionAPI
	{
		private static Regex hashRe = new Regex(@"^http:\/\/((?:www|ar|ru|id|el|pt|tr)\.memegen\.(?:com|de|nl|fr|it|es|se|pl))(\/a)?\/(?:meme|mem|mim)\/([A-Za-z0-9]+)\/?");

		public string GetImageFromUri(Uri uri)
		{
			var href = uri.OriginalString;
			var groups = hashRe.Match(href).Groups;

			if (groups != null)
			{
				if(groups[2].Success)
				{
					return string.Format("http://a.memegen.com/{0}.gif", groups[3].Value);
				}
				else
				{
					return string.Format("http://a.memegen.com/{0}.jpg", groups[3].Value);
				}
			}
			else
				return null;
		}
	}
}
