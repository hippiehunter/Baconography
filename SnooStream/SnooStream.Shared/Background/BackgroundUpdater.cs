using System;
using System.Collections.Generic;
using System.Text;

namespace SnooStream.Background
{
    public class BackgroundUpdater : Windows.ApplicationModel.Background.IBackgroundTask
    {
        public void Run(Windows.ApplicationModel.Background.IBackgroundTaskInstance taskInstance)
        {
            var deferal = taskInstance.GetDeferral();
            try
            {

            }
            finally
            {
                deferal.Complete();
            }
        }
    }
}
