using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using DXGifRenderWP8;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Input;
using SnooStreamWP8.Common;

namespace SnooStreamWP8.View.Controls
{
    public partial class GifControl : UserControl
    {
        public GifControl()
        {
            InitializeComponent();
        }

        Direct3DInterop _interop;
        //this needs to be set to null when you detach it fron the visual hierachy or you will leak memory very rapidly
        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register(
                "ImageSource",
                typeof(object),
                typeof(GifControl),
                new PropertyMetadata(null, onImageSourceSet)
            );


        //this exists because DrawingSurface totally screws up the gesture events for itself and most containing controls
        //this can be safely hosted in something like a RadSlideView if you pass in a ManipulationController and remember to clear it
        //when the control is removed otherwise you will leak like crazy
        public ManipulationController ManipulationController
        {
            get { return (ManipulationController)GetValue(ManipulationControllerProperty); }
            set { SetValue(ManipulationControllerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ManipulationController.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ManipulationControllerProperty =
            DependencyProperty.Register("ManipulationController", typeof(ManipulationController), typeof(GifControl), new PropertyMetadata(null, onControllerSet));

		private TaskCompletionSource<bool> _loadedCompletionSource = new TaskCompletionSource<bool>();

        private static void onControllerSet(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = e.NewValue as ManipulationController;
            var thisp = d as GifControl;
            var oldValue = e.OldValue as ManipulationController;

            if (thisp == null)
                return;

            if (oldValue != null)
            {
                oldValue.DoubleTap -= thisp.viewport_DoubleTap;
                oldValue.ManipulationDelta -= thisp.OnManipulationDelta;
                oldValue.ManipulationCompleted -= thisp.OnManipulationCompleted;
                oldValue.ManipulationStarted -= thisp.OnManipulationStarted;
            }

            if (value != null)
            {
                value.DoubleTap += thisp.viewport_DoubleTap;
                value.ManipulationDelta += thisp.OnManipulationDelta;
                value.ManipulationCompleted += thisp.OnManipulationCompleted;
                value.ManipulationStarted += thisp.OnManipulationStarted;
            }
        }

        private static void onImageSourceSet(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = e.NewValue;
            var thisp = d as GifControl;
            if (value == null && thisp.image != null)
            {
                thisp.image.SetContentProvider(null);
                thisp._interop = null;
            }
            else if (thisp.image != null && thisp._interop == null && value is Task<byte[]>)
            {
                thisp.SetContentProvider(value as Task<byte[]>);
            }
        }

        public object ImageSource
        {
            get { return GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        private async void SetContentProvider(Task<byte[]> asset)
        {
            try
            {
                var bytes = await asset;
				await _loadedCompletionSource.Task;
                _interop = new Direct3DInterop(bytes);
                // Set native resolution in pixels
                _interop.WindowBounds = _interop.RenderResolution = _interop.NativeResolution = new Windows.Foundation.Size(_interop.Width, _interop.Height);
                image.Height = _interop.Height;
                image.Width = _interop.Width;
                // Hook-up native component to DrawingSurface
                image.SetContentProvider(_interop.CreateContentProvider());
                // Set scale to the minimum, and then save it. 
                _scale = 0;
                CoerceScale(true);
                _scale = _coercedScale;

                ResizeImage(true);
            }
            catch
            {
            }
        }

        const double MaxScale = 10;

        double _scale = 1.0;
        double _minScale;
        double _coercedScale;
        double _originalScale;

        Size _viewportSize;
        bool _pinching;
        Point _screenMidpoint;
        Point _relativeMidpoint;

        /// <summary> 
        /// Either the user has manipulated the image or the size of the viewport has changed. We only 
        /// care about the size. 
        /// </summary> 
        void viewport_ViewportChanged(object sender, System.Windows.Controls.Primitives.ViewportChangedEventArgs e)
        {
            Size newSize = new Size(viewport.Viewport.Width, viewport.Viewport.Height);
            if (newSize != _viewportSize)
            {
                _viewportSize = newSize;
                CoerceScale(true);
                ResizeImage(false);
            }
        }

        /// <summary> 
        /// Handler for the ManipulationStarted event. Set initial state in case 
        /// it becomes a pinch later. 
        /// </summary> 
        void OnManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            _pinching = false;
            _originalScale = _scale;
        }

        /// <summary> 
        /// Handler for the ManipulationDelta event. It may or may not be a pinch. If it is not a  
        /// pinch, the ViewportControl will take care of it. 
        /// </summary> 
        /// <param name="sender"></param> 
        /// <param name="e"></param> 
        void OnManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (e.PinchManipulation != null)
            {
                e.Handled = true;

                if (!_pinching)
                {
                    _pinching = true;
                    Point center = e.PinchManipulation.Original.Center;
                    _relativeMidpoint = new Point(center.X / image.ActualWidth, center.Y / image.ActualHeight);

                    var xform = image.TransformToVisual(viewport);
                    _screenMidpoint = xform.Transform(center);
                }

                _scale = _originalScale * e.PinchManipulation.CumulativeScale;

                CoerceScale(false);
                ResizeImage(false);
            }
			else if (_pinching)
			{
				_pinching = false;
				_originalScale = _scale = _coercedScale;
			}
        }

        /// <summary> 
        /// The manipulation has completed (no touch points anymore) so reset state. 
        /// </summary> 
        void OnManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            _pinching = false;
            _scale = _coercedScale;
        }

        /// <summary> 
        /// Adjust the size of the image according to the coerced scale factor. Optionally 
        /// center the image, otherwise, try to keep the original midpoint of the pinch 
        /// in the same spot on the screen regardless of the scale. 
        /// </summary> 
        /// <param name="center"></param> 
        void ResizeImage(bool center)
        {
            if (_coercedScale != 0 && _interop != null)
            {
				double newWidth = Math.Round(_interop.Width * _coercedScale);
				double newHeight = Math.Round(_interop.Height * _coercedScale);

                xform.ScaleX = xform.ScaleY = _coercedScale;

                viewport.Bounds = new Rect(0, 0, newWidth, newHeight);
				Point originPoint;
                if (center)
                {
					originPoint = new Point(Math.Round(newWidth / 2), Math.Round(newHeight / 2));
                }
                else
                {
                    Point newImgMid = new Point(newWidth * _relativeMidpoint.X, newHeight * _relativeMidpoint.Y);
					originPoint = new Point(newImgMid.X - _screenMidpoint.X, newImgMid.Y - _screenMidpoint.Y);
                }

				viewport.SetViewportOrigin(originPoint);
            }
        }

        /// <summary> 
        /// Coerce the scale into being within the proper range. Optionally compute the constraints  
        /// on the scale so that it will always fill the entire screen and will never get too big  
        /// to be contained in a hardware surface. 
        /// </summary> 
        /// <param name="recompute">Will recompute the min max scale if true.</param> 
        void CoerceScale(bool recompute)
        {
            if (recompute && _interop != null && viewport != null)
            {
                // Calculate the minimum scale to fit the viewport 
                double minX = Width / (double)_interop.Width;
                double minY = Height / (double)_interop.Height;

                _minScale = Math.Min(minX, minY);
            }

            _coercedScale = Math.Min(MaxScale, Math.Max(_scale, _minScale));

        }

        private void viewport_DoubleTap(object sender, GestureEventArgs e)
        {
            var point = e.GetPosition(image);
            _relativeMidpoint = new Point(point.X / image.ActualWidth, point.Y / image.ActualHeight);

            var xform = image.TransformToVisual(viewport);
            _screenMidpoint = xform.Transform(point);

            if (_coercedScale >= (_minScale * 2.5) || _coercedScale < 0)
                _coercedScale = _minScale;
            else
                _coercedScale *= 1.75;

            _scale = _coercedScale;

            ResizeImage(false);
        }

		private async void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			//make sure we're really loaded
			while(viewport.ActualHeight < 1)
				await Task.Yield();

			_viewportSize = new Size(viewport.ActualWidth, viewport.ActualHeight);
			_loadedCompletionSource.TrySetResult(true);
		}
    }
}
