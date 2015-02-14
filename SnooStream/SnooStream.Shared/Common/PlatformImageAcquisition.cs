﻿using CommonResourceAcquisition.ImageAcquisition;
using MetroLog;
using SnooStream.PlatformServices;
using SnooStream.ViewModel;
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

            try
            {
                return await SnooStreamBackground.ImageUtilities.MakeSizedImage("snoostream_preview", url, 300, 768).AsTask(cancelToken);
            }
            catch (OperationCanceledException)
            {
                throw new TaskCanceledException();
            }
            catch (Exception ex)
            {
                if (((uint)ex.HResult == 0x80004004))
                {
                    throw new TaskCanceledException();
                }
                else if (((uint)ex.HResult) != 0x800700B7)
                {
                    _logger.Error("failed getting image content", ex);
                    return null;
                }

            }

            _logger.Error("failed getting image content");
            return null;
		}
       
    }
}
