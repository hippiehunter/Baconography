﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using SnooStream.ViewModel;
using System.Collections.ObjectModel;
using Telerik.Windows.Controls;
using SnooStreamWP8.Common;
using System.Threading.Tasks;
using System.Diagnostics;
using Telerik.Windows.Controls.SlideView;
using SnooStreamWP8.View.Controls;
using System.Windows.Media.Imaging;

namespace SnooStreamWP8.View.Pages
{
    public partial class LinkStream : SnooApplicationPage
    {
        public LinkStream()
        {
            InitializeComponent();
        }

        bool _noMoreLoad = false;
        bool _noMoreLoadBack = false;
        ObservableCollection<ContentViewModel> _links;
        public ObservableCollection<ContentViewModel> Links
        {
            get
            {
                if (_links == null)
                {
                    ((IPageProvider)radSlideView).CurrentIndexChanged += radSlideViewIndexChanged;
                    _links = new ObservableCollection<ContentViewModel>();
                    LoadInitialLinks(_links);
                }
                return _links;
            }
        }

        bool _loading = false;

        public async void LoadInitialLinks(ObservableCollection<ContentViewModel> links)
        {
            try
            {
                _loading = true;
                var linkStream = DataContext as LinkStreamViewModel;
                if (linkStream != null && await linkStream.MoveNext())
                {
                    AddLoadingLink(links, linkStream.Current, false);
                    for (int i = 0; i < 5 && await linkStream.MoveNext(); i++)
                    {
                        AddLoadingLink(links, linkStream.Current, false);
                    }

                    await Task.Yield();
                    var backLinkCount = 0;
                    LinkViewModel currentPrior;
                    while (linkStream.LoadPrior.Value != null && 
                        (currentPrior = (await linkStream.LoadPrior.Value.Next()) as LinkViewModel) != null && 
                        backLinkCount < 5)
                    {
                        AddLoadingLink(links, currentPrior, true);
                    }
                }
            }
            finally
            {
                _loading = false;
            }
        }

        private void AddLoadingLink(ObservableCollection<ContentViewModel> links, LinkViewModel link, bool first)
        {
            if (link == null)
            {
                Debug.WriteLine("something tried to add a null link for loading content {0}", new StackTrace().ToString());
                return;
            }
            LoadingContentViewModel loadingVM;
            Task fullyLoaded;
            if (link.Content != null)
            {
                loadingVM = new LoadingContentViewModel(link.Content);
                fullyLoaded = link.Content.BeginLoad(true);
            }
            else
            {
                loadingVM = new LoadingContentViewModel(link.AsyncContent, DataContext as GalaSoft.MvvmLight.ViewModelBase);
                fullyLoaded = link.AsyncContent.ContinueWith(tsk => 
                    {
                        if(tsk.Status == TaskStatus.RanToCompletion)
                        {
                            return tsk.Result.BeginLoad(true);
                        }
                        else
                            return Task.FromResult<bool>(false);
                    });
            }

            if (first)
            {
                links.Insert(0, loadingVM);
            }
            else
            {
                links.Add(loadingVM);
            }

            fullyLoaded.ContinueWith(tsk =>
                {
                    if (link.Content != null && !link.Content.Errored)
                    {
                        links[links.IndexOf(loadingVM)] = link.Content;
                    }
                    else
                    {
                        Debug.WriteLine("failed to add a link after the task should have been finished");
                    }
                }, SnooStreamViewModel.UIScheduler);
        }

        SlideViewItem currentItem;
        private async void radSlideViewIndexChanged(object sender, EventArgs e)
        {
            if (currentItem != null)
            {
                currentItem.Content = null;
                currentItem = null;
            }
            if (radSlideView.SelectedItemContainer != null)
            {
                currentItem = radSlideView.SelectedItemContainer;
            }
            if(Links.Count > 0 && !_loading)
            {
                try
                {
                    _loading = true;
                    //preload distance
                    if (!_noMoreLoad && ((IPageProvider)radSlideView).CurrentIndex > (Links.Count - 5))
                    {
                        _noMoreLoad = !(await ((LinkStreamViewModel)DataContext).MoveNext());
                        if (!_noMoreLoad)
                        {
                            AddLoadingLink(Links, ((LinkStreamViewModel)DataContext).Current, false);
                        }
                    }
                    if (!_noMoreLoadBack && ((IPageProvider)radSlideView).CurrentIndex <= 5)
                    {
                        var backEnum = ((LinkStreamViewModel)DataContext).LoadPrior.Value;
                        var currentPrior = await backEnum.Next() as LinkViewModel;
                        _noMoreLoadBack = currentPrior == null;
                        if (!_noMoreLoadBack)
                        {
                            AddLoadingLink(Links, currentPrior, true);
                        }
                    }
                }
                finally
                {
                    _loading = false;
                }
            }
        }

        private void PanAndZoomImage_Unloaded(object sender, RoutedEventArgs e)
        {
            var pZoom = sender as PanAndZoomImage;
            if (pZoom.Source is BitmapImage)
            {
                ((BitmapImage)pZoom.Source).UriSource = null;
            }
            pZoom.Source = null;
        }

        private void GifControl_Unloaded(object sender, RoutedEventArgs e)
        {
            var gControl = sender as GifControl;
            gControl.ImageSource = null;
        }

        private void radSlideView_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {

        }
    }
}