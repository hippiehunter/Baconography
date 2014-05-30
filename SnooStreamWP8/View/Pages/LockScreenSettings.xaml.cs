using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using SnooStreamWP8.Common;
using SnooStream.ViewModel;
using GalaSoft.MvvmLight;
using SnooStreamWP8.BackgroundControls.ViewModel;
using SnooStream.Model;
using System.ComponentModel;

namespace SnooStreamWP8.View.Pages
{
	public partial class LockScreenSettings : SnooApplicationPage
	{
		public LockScreenSettings()
		{
			InitializeComponent();
		}

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // If we have lockscreens, do nothing

            // If we don't, and have access to network, fetch one

            // If we have nothing, use default

            var vm = this.DataContext as SettingsViewModel;
            if (vm != null)
            {
                vm.LockScreen.SelectedImage = "/Assets/RainbowGlass.jpg";
                _previewLockScreenViewModel = new PreviewLockScreenViewModel(vm.LockScreen);
                SetValue(PreviewLockScreenVMProperty, _previewLockScreenViewModel);
            }
        }

        public static readonly DependencyProperty PreviewLockScreenVMProperty =
            DependencyProperty.Register(
                "PreviewLockScreenVM", typeof(PreviewLockScreenViewModel),
                typeof(LockScreenSettings),
                new PropertyMetadata(null)
            );

        private PreviewLockScreenViewModel _previewLockScreenViewModel;
        public PreviewLockScreenViewModel PreviewLockScreenVM
        {
            get
            {
                return _previewLockScreenViewModel;
            }
        }

        public static readonly DependencyProperty IsLockScreenProviderProperty =
            DependencyProperty.Register(
                "IsLockScreenProvider", typeof(bool),
                typeof(LockScreenSettings),
                new PropertyMetadata(false)
            );

        public bool IsLockScreenProvider
        {
            get
            {
                return Windows.Phone.System.UserProfile.LockScreenManager.IsProvidedByCurrentApplication;
            }
        }

        private void SetLockScreenProvider_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {

        }

        public class PreviewLockScreenViewModel : ViewModelBase
        {
            SnooStream.ViewModel.LockScreenViewModel _lockScreenVM;
            public PreviewLockScreenViewModel(SnooStream.ViewModel.LockScreenViewModel lockScreenVM)
            {
                _lockScreenVM = lockScreenVM;

                _lockScreenVM.PropertyChanged += PropertyChangedHandler;

                _overlayItems = new List<LockScreenMessage>();
                if (_overlayItems.Count == 0 ||
                    (_overlayItems.Count > 0 && _overlayItems.First().Glyph != Utility.UnreadMailGlyph))
                {
                    this._overlayItems.Insert(0, new LockScreenMessage { Glyph = Utility.UnreadMailGlyph, DisplayText = "Sample unread message" });
                }

                var SampleCollection = new List<LockScreenMessage> {
                    new LockScreenMessage { Glyph = LinkGlyphUtility.PhotoGlyph, DisplayText = "The funniest picture on the front page" },
                    new LockScreenMessage { Glyph = LinkGlyphUtility.WebGlyph, DisplayText = "Very interesting article about cats" },
                    new LockScreenMessage { Glyph = LinkGlyphUtility.DetailsGlyph, DisplayText = "I am the walrus, AMA" },
                    new LockScreenMessage { Glyph = LinkGlyphUtility.VideoGlyph, DisplayText = "I am proud to present a short film about film critics" },
                    new LockScreenMessage { Glyph = LinkGlyphUtility.MultiredditGlyph, DisplayText = "A multireddit of all of the best stuff that reddit has to offer" },
                    new LockScreenMessage { Glyph = LinkGlyphUtility.PhotoGlyph, DisplayText = "Breathtaking vista of a massive canyon" }
                };

                this._overlayItems.AddRange(SampleCollection);
            }

            private void PropertyChangedHandler(object sender, PropertyChangedEventArgs e)
            {
                switch (e.PropertyName)
                {
                    case "OverlayItemCount":
                        {
                            RaisePropertyChanged("OverlayItemCount");
                            RaisePropertyChanged("OverlayItems");
                            RaisePropertyChanged("UseLockScreenOverlay");
                            break;
                        }
                    case "MessagesInLockScreenOverlay":
                        {
                            RaisePropertyChanged("MessagesInLockScreenOverlay");
                            RaisePropertyChanged("OverlayItems");
                            RaisePropertyChanged("UseLockScreenOverlay");
                            break;
                        }
                    case "PostsInLockScreenOverlay":
                        {
                            RaisePropertyChanged("PostsInLockScreenOverlay");
                            RaisePropertyChanged("OverlayItems");
                            RaisePropertyChanged("UseLockScreenOverlay");
                            break;
                        }
                    case "RoundedLockScreen":
                        {
                            RaisePropertyChanged("RoundedLockScreen");
                            RaisePropertyChanged("CornerRadius");
                            RaisePropertyChanged("Margin");
                            RaisePropertyChanged("InnerMargin");
                            break;
                        }
                    case "OverlayOpacity":
                        {
                            RaisePropertyChanged("OverlayOpacity");
                            break;
                        }

                    case "UseLockScreenOverlay":
                        {
                            RaisePropertyChanged("UseLockScreenOverlay");
                            break;
                        }
                }
            }

            List<LockScreenMessage> _overlayItems;
            public List<LockScreenMessage> OverlayItems
            {
                get
                {
                    List<LockScreenMessage> collection = new List<LockScreenMessage>();
                    if (MessagesInLockScreenOverlay)
                        collection.AddRange(_overlayItems.Where(p => p.Glyph == Utility.UnreadMailGlyph));
                    if (PostsInLockScreenOverlay)
                        collection.AddRange(_overlayItems.Where(p => p.Glyph != Utility.UnreadMailGlyph));

                    return collection.Take(OverlayItemCount).ToList();
                }
                set
                {
                    _overlayItems = value;
                    RaisePropertyChanged("OverlayItems");
                }
            }

            public bool UseLockScreenOverlay
            {
                get
                {
                    if (OverlayItemCount == 0)
                        return false;
                    if (!MessagesInLockScreenOverlay
                        && !PostsInLockScreenOverlay)
                        return false;
                    return _lockScreenVM.UseLockScreenOverlay;
                }
            }

            public int OverlayItemCount
            {
                get
                {
                    return _lockScreenVM.OverlayItemCount;
                }
            }

            public bool MessagesInLockScreenOverlay
            {
                get
                {
                    return _lockScreenVM.MessagesInLockScreenOverlay;
                }
            }

            public bool PostsInLockScreenOverlay
            {
                get
                {
                    return _lockScreenVM.PostsInLockScreenOverlay;
                }
            }

            public bool RoundedLockScreen
            {
                get
                {
                    return _lockScreenVM.RoundedLockScreen;
                }
            }

            public CornerRadius CornerRadius
            {
                get
                {
                    if (RoundedLockScreen)
                        return new CornerRadius(5);
                    return new CornerRadius(0);
                }
            }

            public Thickness Margin
            {
                get
                {
                    if (RoundedLockScreen)
                        return new Thickness(12, 40, 12, 12);
                    return new Thickness(-5, 40, -5, 0);
                }
            }

            public Thickness InnerMargin
            {
                get
                {
                    if (RoundedLockScreen)
                        return new Thickness(0, 0, 0, 0);
                    return new Thickness(17, 0, 17, 0);
                }
            }

            public float OverlayOpacity
            {
                get
                {
                    return _lockScreenVM.OverlayOpacity;
                }
            }
        }
	}
}