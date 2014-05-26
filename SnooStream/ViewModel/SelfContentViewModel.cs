using GalaSoft.MvvmLight;
using SnooSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.ViewModel
{
    public class SelfContentViewModel : ContentViewModel
    {
        public SelfContentViewModel(ViewModelBase context, Link link) : base(context)
        {
            Link = link;
            PreviewText = NBoilerpipePortable.Util.HttpUtility.HtmlDecode(link.Selftext).Replace("\t", "").Replace("\n", "");
            PreviewImage = null;
        }

        public Link Link { get; private set; }

		internal override async Task LoadContent(bool previewOnly, Action<int> progress, CancellationToken cancelToken)
        {
            //load the comments
        }
    }
}
