using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommonImageAquisition.SimpleAPI
{
	class Ctrlv : IAquisitionAPI
	{
		private static Regex hashRe = new Regex(@"^http:\/\/((m|www)\.)?ctrlv\.in\/([0-9]+)");

		public string GetImageFromUri(Uri uri)
		{
			var href = uri.OriginalString;
			var groups = hashRe.Match(href).Groups;

			if (groups != null)
				return string.Format("http://img.ctrlv.in/id/{0}.jpg", groups[3].Value);

			else
				return null;
		}
	}
}
