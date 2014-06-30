using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Input;

namespace SnooStream.Common
{
    public class ManipulationController
    {
        public event DoubleTappedEventHandler DoubleTap;
        public event ManipulationStartedEventHandler ManipulationStarted;
        public event ManipulationDeltaEventHandler ManipulationDelta;
        public event ManipulationCompletedEventHandler ManipulationCompleted;

        public void FireDoubleTap(object obj, DoubleTappedRoutedEventArgs args)
        {
            
            if (DoubleTap != null)
                DoubleTap(obj, args);
        }

        public void FireManipulationStarted(object obj, ManipulationStartedRoutedEventArgs args)
        {
            if (ManipulationStarted != null)
                ManipulationStarted(obj, args);
        }

        public void FireManipulationDelta(object obj, ManipulationDeltaRoutedEventArgs args)
        {
            if (ManipulationDelta != null)
                ManipulationDelta(obj, args);
        }

        public void FireManipulationCompleted(object obj, ManipulationCompletedRoutedEventArgs args)
        {
            if (ManipulationCompleted != null)
                ManipulationCompleted(obj, args);
        }
    }
}
