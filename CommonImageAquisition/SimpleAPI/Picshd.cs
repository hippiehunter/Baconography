using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommonImageAquisition.SimpleAPI
{
	class Picshd : IAquisitionAPI
	{
		private static Regex hashRe = new Regex(@"^https?:\/\/(?:i\.|edge\.|www\.)*picshd\.com\/([\w]{5,})(\..+)?$");

		public string GetImageFromUri(Uri uri)
		{
			var href = uri.OriginalString;
			var groups = hashRe.Match(href).Groups;

			if (groups != null)
				return string.Format("http://i.picshd.com/{0}.jpg", groups[1].Value);

			else
				return null;
		}
	}
}
