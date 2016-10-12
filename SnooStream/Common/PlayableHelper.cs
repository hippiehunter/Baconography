using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SnooStream.Common
{
    public class PlayableHelper : DependencyObject
    {
        public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.RegisterAttached(
            "IsPlaying",
            typeof(bool),
            typeof(PlayableHelper), new PropertyMetadata(null, IsPlayingChanged)
            );

        private static void IsPlayingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = d as MediaElement;
            var isPlaying = e.NewValue as bool?;

            if (isPlaying ?? false)
            {
                try
                {
                    target.Play();
                }
                catch { }
                target.AutoPlay = true;
            }
            else
            {
                try
                {
                    target.Stop();
                }
                catch { }
                target.AutoPlay = false;
            }
        }

        public static void SetIsPlaying(UIElement element, object value)
        {
            element.SetValue(IsPlayingProperty, value);
        }

        public static object GetIsPlaying(UIElement element)
        {
            return element.GetValue(IsPlayingProperty);
        }
    }
}
