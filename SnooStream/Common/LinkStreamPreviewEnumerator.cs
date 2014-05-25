using CommonImageAquisition;
using CommonVideoAquisition;
using SnooStream.Model;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public class LinkStreamPreviewEnumerator : ObservableCollection<LinkViewModel>
    {
        public static async Task<LinkStreamPreviewEnumerator> MakePreviewEnumerator(LinkRiverViewModel context, LinkStreamViewModel linkStream)
        {
            int imagePreviewableCount = 0;
            //check the first 20 links to see what kind of content they are
            for (int i = 0; i < 20 && await linkStream.MoveNext(); i++)
            {
                if(ImageAquisition.IsImageAPI(linkStream.Current.Url) ||
                    VideoAquisition.IsAPI(linkStream.Current.Url))
                {
                    imagePreviewableCount++;
                }
            }

            var previewEnumerator = new LinkStreamPreviewEnumerator(context, linkStream, imagePreviewableCount > 10);
            if (previewEnumerator.IsImagePreview)
            {
                var queuedTask = SnooStreamViewModel.LoadQueue.QueueLoadItem(context.Thing.Url, LoadContextType.Minor, () => FillPreviewEnumerator(previewEnumerator, 10, SnooStreamViewModel.UIContextCancellationToken));
            }
            return previewEnumerator;
        }

        private void AddContent(LinkViewModel viewModel)
        {
            Add(viewModel);
        }

        public static async Task FillPreviewEnumerator(LinkStreamPreviewEnumerator stream, int fillContentCount, CancellationToken cancelToken)
        {
            int i = 0;
            while (fillContentCount > i && await stream._linkStream.MoveNext())
            {
                if (stream._linkStream.Current.Content != null)
                {
                    i++;
                    await stream._linkStream.Current.Content.BeginPreviewLoad().ContinueWith((tsk) => stream.AddContent(stream._linkStream.Current), SnooStreamViewModel.UIScheduler);
                }   
            }        
        }
        public LinkStreamPreviewEnumerator(LinkRiverViewModel context, LinkStreamViewModel linkStream, bool isImagePreview)
        {
            IsImagePreview = isImagePreview;
            _linkStream = linkStream;
            _context = context;
        }

        //when this gets bound it means someone is looking at us, so its reasonable to put ourselves in line to be loaded
        public LinkStreamPreviewEnumerator ContextBinding
        {
            get
            {
                return this;
            }
        }

        private LinkRiverViewModel _context;
        private LinkStreamViewModel _linkStream;
        public bool IsImagePreview { get; set; }
    }
}
