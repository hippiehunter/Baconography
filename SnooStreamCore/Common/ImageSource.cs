using CommonResourceAcquisition.ImageAcquisition;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public class ImageSource
    {
        public JpegUtility.Dimensions Dimensions { get; private set; }
        public string UrlSource { get; private set; }
        public ImageSource(string urlSource, byte[] initialData)
        {
            UrlSource = urlSource;
            if (initialData != null)
            {
                using (var stream = new MemoryStream(initialData))
                {
                    Dimensions = JpegUtility.GetJpegDimensions(stream);
                }
                _cachedData = new WeakReference<byte[]>(initialData);
            }
        }

        protected WeakReference<byte[]> _cachedData;

        public virtual Task<byte[]> ImageData
        {
            get
            {
                byte[] data;
                if (_cachedData != null && _cachedData.TryGetTarget(out data))
                    return Task.FromResult(data);
                else
                {
                    return Task.Run<byte[]>(async () =>
                        {
                            using (var handler = new HttpClientHandler())
                            {
                                using (var client = new HttpClient())
                                {
                                    
                                    _cachedData = new WeakReference<byte[]>(data = await client.GetByteArrayAsync(UrlSource));
                                    return data;
                                }
                            }
                        });
                }
            }
        }
    }

    public class PreviewImageSource : ImageSource
    {
        public PreviewImageSource(ImageSource mainSource) : base(mainSource.UrlSource, null)
        {
            ImageSource = mainSource;
        }

        public ImageSource ImageSource { get; set; }
        public override Task<byte[]> ImageData
        {
            get
            {
                byte[] resizedBytes;
                if (_cachedData != null && _cachedData.TryGetTarget(out resizedBytes))
                    return Task.FromResult(resizedBytes);
                else
                {
                    return Task.Run<byte[]>(async () =>
                        {
                            var mainBytes = await ImageSource.ImageData;
                            _cachedData = new WeakReference<byte[]>(resizedBytes = await SnooStreamViewModel.SystemServices.ResizeImage(mainBytes, 691, 336));
                            return resizedBytes;
                        });
                }
            }
        }
    }
}
