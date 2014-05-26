using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommonImageAquisition.SimpleAPI
{
	class Ehost : IAquisitionAPI
	{
		private static Regex hashRe = new Regex(@"^http:\/\/(?:i\.)?(?:\d+\.)?eho\.st\/(\w+)\/?");

		public string GetImageFromUri(Uri uri)
		{
			var href = uri.OriginalString;
			var groups = hashRe.Match(href).Groups;

			if (groups != null)
				return string.Format("http://i.eho.st/{0}.jpg", groups[1].Value);

			else
				return null;
		}
	}
}
