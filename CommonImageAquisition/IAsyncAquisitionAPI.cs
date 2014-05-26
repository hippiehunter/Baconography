using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonImageAquisition
{
	interface IAsyncAquisitionAPI
	{
		bool IsMatch(Uri uri);
		Task<IEnumerable<Tuple<string, string>>> GetImagesFromUri(string title, Uri uri);
	}
}
