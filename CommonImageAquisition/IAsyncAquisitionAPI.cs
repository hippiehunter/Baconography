using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonImageAcquisition
{
	interface IAsyncAcquisitionAPI
	{
		bool IsMatch(Uri uri);
		Task<IEnumerable<Tuple<string, string>>> GetImagesFromUri(string title, Uri uri);
	}
}
