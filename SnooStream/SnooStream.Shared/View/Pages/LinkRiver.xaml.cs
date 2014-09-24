using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using SnooStream.Common;
using SnooStream.ViewModel;
using Windows.UI.Xaml.Data;
using GalaSoft.MvvmLight.Messaging;
using SnooStream.Messages;
using Windows.UI.Xaml;

namespace SnooStream.View.Pages
{
    public partial class LinkRiver : SnooApplicationPage
    {
        public LinkRiver()
        {
            InitializeComponent();
			var linksViewSource = LayoutRoot.Resources["linksViewSource"] as CollectionViewSource;
			Messenger.Default.Register<SelectLinkMessage>(this, act = (message) =>
				{
					if (linksViewSource.View.Contains(message.Link))
					{
						linksViewSource.View.MoveCurrentTo(message.Link);
						PushNavState(this, message.Kind == SelectLinkMessage.LinkSelectionKind.Content ?
							"FlipView" : "CommentsView");
					}
				});
        }
		Action<SelectLinkMessage> act;
    }
}