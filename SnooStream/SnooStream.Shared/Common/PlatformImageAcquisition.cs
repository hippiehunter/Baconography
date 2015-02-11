﻿using CommonResourceAcquisition.ImageAcquisition;
using MetroLog;
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
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Web.Http;

namespace SnooStream.Common
{
    class PlatformImageAcquisition : ImageAcquisition
    {
		static protected ILogger _logger = LogManagerFactory.DefaultLogManager.GetLogger<ImageAcquisition>();
		
		public static string ComputeMD5(string str)
		{
			var alg = HashAlgorithmProvider.OpenAlgorithm("MD5");
			IBuffer buff = CryptographicBuffer.ConvertStringToBinary(str, BinaryStringEncoding.Utf8);
			var hashed = alg.HashData(buff);
			var res = CryptographicBuffer.EncodeToHexString(hashed);
			return res;
		}

		private static void RegisterCancel(CancellationToken cancelToken, IAsyncInfo asyncOperation)
		{
			WeakReference<object> _weakRef = new WeakReference<object>(asyncOperation);
			cancelToken.Register(() =>
				{
					try
					{
						object target;
						if (_weakRef.TryGetTarget(out target))
						{
                            if(target is IAsyncInfo)
							    ((IAsyncInfo)target).Cancel();
						}
						_weakRef = null;
					}
					catch 
					{
						_weakRef = null;
					}
				});
		}

		public static async Task<String> ImagePreviewFromUrl(string url, CancellationToken cancelToken)
		{
            if (string.IsNullOrWhiteSpace(url))
                return null;

			string onDiskName = "snoostream_preview" + ComputeMD5(url);
			try
			{
                if (SnooStreamBackground.ImageUtilities.FileExists(ApplicationData.Current.TemporaryFolder.Path + "\\" + onDiskName))
                {
                    return ApplicationData.Current.TemporaryFolder.Path + "\\" + onDiskName;
                }

                var targetFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(onDiskName, CreationCollisionOption.FailIfExists);
				Exception failed = null;
				try
				{
					using (var targetStream = await targetFile.OpenAsync(FileAccessMode.ReadWrite))
					{
						cancelToken.ThrowIfCancellationRequested();
						using (var client = new HttpClient())
						{
							var asyncHttpOp = client.GetAsync(new Uri(url), HttpCompletionOption.ResponseHeadersRead);
							RegisterCancel(cancelToken, asyncHttpOp);
							using (var response = await asyncHttpOp)
							{
								using (var content = response.Content)
								{
                                    if (content.Headers.ContentLength != null && content.Headers.ContentLength.Value > (1024 * 1024 * 4))
										throw new OperationCanceledException();

									var readBufferOp = content.ReadAsBufferAsync();
									RegisterCancel(cancelToken, readBufferOp);
									var buffer = await readBufferOp;
									try
									{
										cancelToken.ThrowIfCancellationRequested();

										using (var source = new BufferImageSource(buffer))
										{
											var info = await source.GetInfoAsync();

                                            if (source.ImageFormat == ImageFormat.Jpeg && (info.ImageSize.Height > 768 || info.ImageSize.Width > 768))
											{
                                                var resizedBuffer = await Nokia.Graphics.Imaging.JpegTools.AutoResizeAsync(buffer, new Nokia.Graphics.Imaging.AutoResizeConfiguration(768 * 768,
                                                new Windows.Foundation.Size(768, 768), new Windows.Foundation.Size(0, 0), Nokia.Graphics.Imaging.AutoResizeMode.Automatic, 0, Nokia.Graphics.Imaging.ColorSpace.Yuv420));
												try
												{
													await targetStream.WriteAsync(resizedBuffer);
												}
												finally
												{
													if (resizedBuffer is IDisposable)
														((IDisposable)resizedBuffer).Dispose();
												}
											}
                                            else if (info.ImageSize.Height > 768 || info.ImageSize.Width > 768 || source.ImageFormat == ImageFormat.Gif)
											{
												using (var jpegRenderer = new JpegRenderer(source))
												{
													// Find aspect ratio for resize
                                                    var nPercentW = (768 / info.ImageSize.Width);
                                                    var nPercentH = (768 / info.ImageSize.Height);
													var nPercent = nPercentH < nPercentW ? nPercentH : nPercentW;

													jpegRenderer.Size = new Windows.Foundation.Size(info.ImageSize.Width * nPercent, info.ImageSize.Height * nPercent);
													jpegRenderer.OutputOption = OutputOption.PreserveAspectRatio;
													jpegRenderer.Quality = .50;
													var renderedJpeg = await jpegRenderer.RenderAsync();
													try
													{
														await targetStream.WriteAsync(renderedJpeg);
													}
													finally
													{
														if (renderedJpeg is IDisposable)
															((IDisposable)renderedJpeg).Dispose();
													}
												}
											}
											else
												await targetStream.WriteAsync(buffer);
										}
									}
									finally
									{
										if (buffer is IDisposable)
											((IDisposable)buffer).Dispose();
									}
								}
							}

						}
					}
				}
				catch (Exception ex)
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
				throw new TaskCanceledException();
			}
			catch(Exception ex) 
			{
				if (((uint)ex.HResult) != 0x800700B7)
				{
					_logger.Error("failed getting image content", ex);
					return null;
				}
			}
			return ApplicationData.Current.TemporaryFolder.Path + "\\" + onDiskName;
		}
    }
}
