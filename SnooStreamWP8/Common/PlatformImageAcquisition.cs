using CommonImageAcquisition;
using SnooStreamWP8.PlatformServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SnooStreamWP8.Common
{
    class PlatformImageAcquisition : ImageAcquisition
    {
        public static async Task<byte[]> ImageBytesFromUrl(string url, bool isRetry)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                using (WebResponse response = await SystemServices.GetResponseAsync(request))
                {
                    if (response == null)
                        return null;

                    using (Stream imageStream = response.GetResponseStream())
                    {
                        using (var result = new MemoryStream())
                        {
                            await imageStream.CopyToAsync(result);
                            return result.ToArray();
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                if (isRetry || !System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                    return null;
            }

            //delay a bit and try again
            await Task.Delay(500);
            return await ImageBytesFromUrl(url, true);
        }

        public static async Task<Stream> ImageStreamFromUrl(string url)
        {
            string targetUrl = url;
            if (IsImage(url) && IsImageAPI(url))
            {
                var imageApiResults = await ImageAcquisition.GetImagesFromUrl("", url);
                if (imageApiResults != null && imageApiResults.Count() > 1)
                {
                    targetUrl = imageApiResults.First().Item2;
                }
                else if (imageApiResults != null && imageApiResults.Count() == 1)
                {

                }
            }

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.AllowReadStreamBuffering = true;
            using (WebResponse response = await SystemServices.GetResponseAsync(request))
            {
                return response.GetResponseStream();
            }
        }
    }
}
