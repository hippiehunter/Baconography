using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonVideoAquisition
{
	public class Liveleak
	{
		public static async Task<VideoResult> GetPlayableStreams(string originalUrl, Func<string, Task<string>> getter)
		{
			string pageContents = null;
			if (originalUrl.Contains("&ajax=1"))
				pageContents = await getter(originalUrl);
			else
				pageContents = await getter(originalUrl + "&ajax=1");

			if (pageContents != null)
			{
				var targetString = "file: \"http://edge.liveleak.com";
				var targetStringStart = pageContents.IndexOf(targetString);
				if (targetStringStart != -1)
				{
					var endofTargetString = pageContents.IndexOf("\",", targetStringStart);
					if (endofTargetString != -1)
					{
						targetStringStart += "file: \"".Length;
						var fileUrl = pageContents.Substring(targetStringStart, endofTargetString - targetStringStart);
						return new VideoResult { PlayableStreams = new List<Tuple<string, string>> { Tuple.Create(fileUrl, "mp4") }, PreviewUrl = "" };
					}
				}
			}
			
			return null;
		}
	}
}
