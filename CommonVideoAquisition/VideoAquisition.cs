using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonVideoAquisition
{
    public class VideoAquisition
    {
        
        //someday we will support other video providers
        public static bool IsAPI(string originalUrl)
        {
            var isYoutube = YouTube.IsAPI(originalUrl);
			if (!isYoutube)
			{
				var uri = new Uri(originalUrl);
                var targetHost = uri.DnsSafeHost.ToLower();
				return targetHost == "liveleak.com" ||
					targetHost == "www.liveleak.com";
			}
			else
				return true;
        }

        public static Task<VideoResult> GetPlayableStreams(string originalUrl, Func<string, Task<string>> getter)
        {
			if (YouTube.IsAPI(originalUrl))
				return YouTube.GetPlayableStreams(originalUrl, getter);
			else
			{
				var uri = new Uri(originalUrl);
				var targetHost = uri.DnsSafeHost.ToLower();

				var isLiveLeak = targetHost == "liveleak.com" ||
					targetHost == "www.liveleak.com";

				if (isLiveLeak)
				{
					return Liveleak.GetPlayableStreams(originalUrl, getter);
				}
			}

			return null;
        }
    }
}
