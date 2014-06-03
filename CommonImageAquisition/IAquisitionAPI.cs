using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonImageAcquisition
{
	interface IAcquisitionAPI
	{
		string GetImageFromUri(Uri uri);
	}
}
