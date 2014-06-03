using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommonImageAcquisition.SimpleAPI
{
	class Imgflip : IAcquisitionAPI
	{
		private static Regex hashRe = new Regex(@"^https?:\/\/imgflip\.com\/(i|gif)\/[a-z0-9]+");

		public string GetImageFromUri(Uri uri)
		{
			var href = uri.OriginalString;
			var groups = hashRe.Match(href).Groups;

			if (groups != null)
				return string.Format("https://i.imgflip.com/{0}.{1}", groups[2].Value, groups[1].Value == "gif" ? "gif" : "jpg");

			else
				return null;
		}
	}
}
