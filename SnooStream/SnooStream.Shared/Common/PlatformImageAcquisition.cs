using CommonResourceAcquisition.ImageAcquisition;
using Nokia.Graphics.Imaging;
using SnooStream.PlatformServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Web.Http;

namespace SnooStream.Common
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
            catch (WebException)
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

		public static async Task<String> ImagePreviewFromUrl(string url, CancellationToken cancelToken)
		{
			string onDiskName = "snoostream_preview5" + Uri.EscapeDataString(url);
			try
			{
				var targetFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(onDiskName, CreationCollisionOption.FailIfExists);
				Exception failed = null;
				try
				{
					using (var targetStream = await targetFile.OpenAsync(FileAccessMode.ReadWrite))
					{
						cancelToken.ThrowIfCancellationRequested();
						using (var client = new HttpClient())
						{
							var asyncHttpOp = client.GetAsync(new Uri(url), HttpCompletionOption.ResponseContentRead);
							cancelToken.Register(() =>
								{
									try
									{
										asyncHttpOp.Cancel();
									}
									catch { }
								});
							using (var response = await asyncHttpOp)
							{
								var buffer = await response.Content.ReadAsBufferAsync();
								cancelToken.ThrowIfCancellationRequested();
								using (var source = new BufferImageSource(buffer))
								{
									var info = await source.GetInfoAsync();
									if (info.ImageSize.Height > 1024 || info.ImageSize.Width > 1024)
									{
										var resizedBuffer = await Nokia.Graphics.Imaging.JpegTools.AutoResizeAsync(buffer, new Nokia.Graphics.Imaging.AutoResizeConfiguration(1024 * 1024 * 2,
											new Windows.Foundation.Size(1024, 1024), new Windows.Foundation.Size(0, 0), Nokia.Graphics.Imaging.AutoResizeMode.Automatic, 0, Nokia.Graphics.Imaging.ColorSpace.Yuv420));
										await targetStream.WriteAsync(resizedBuffer);
									}
									else
										await targetStream.WriteAsync(buffer);
								}
							}

						}
					}
				}
				catch(Exception ex)
				{
					failed = ex;
				}
				if (failed != null)
				{
					await targetFile.DeleteAsync();
					throw failed;
				}

			}
			catch (OperationCanceledException cancel)
			{
				throw cancel;
			}
			catch { }
			return ApplicationData.Current.TemporaryFolder.Path + "\\" + onDiskName;
		}
    }
}
