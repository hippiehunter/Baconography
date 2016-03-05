using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace SnooStream.Common
{
    public class DispatchTimer
    {
        private CoreDispatcher _uiDispatcher;
        public DispatchTimer(CoreDispatcher uiDispatcher)
        {
            _uiDispatcher = uiDispatcher;
        }

        public async void StopTimer(object tickHandle)
        {
            if (tickHandle is DispatcherTimer)
            {
                if (((DispatcherTimer)tickHandle).IsEnabled)
                    ((DispatcherTimer)tickHandle).Stop();
            }
            else if (tickHandle is Task<DispatcherTimer>)
            {
                await _uiDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    var timer = await (Task<DispatcherTimer>)tickHandle;
                    timer.Stop();
                });
            }
            else if (tickHandle is ThreadPoolTimer)
            {
                ((ThreadPoolTimer)tickHandle).Cancel();
            }
        }

        public async void RunAsync(Func<object, Task> action)
        {
            await AsyncInfo.Run((c) => action(c));
        }

        public object StartTimer(EventHandler<object> tickHandler, TimeSpan tickSpan, bool uiThread)
        {
            if (uiThread)
            {
                if (tickSpan.Ticks == 0)
                {
                    var asyncItem = _uiDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => tickHandler(null, null));
                    return null;
                }
                else
                {

                    TaskCompletionSource<DispatcherTimer> completionSource = new TaskCompletionSource<DispatcherTimer>();
                    var unusedTask = _uiDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        DispatcherTimer dt = new DispatcherTimer();
                        dt.Tick += (sender, args) => tickHandler(sender, args);
                        dt.Interval = tickSpan;
                        dt.Start();
                        completionSource.SetResult(dt);
                    });
                    return completionSource.Task;
                }
            }
            else
            {
                return ThreadPoolTimer.CreatePeriodicTimer((timer) => tickHandler(this, timer), tickSpan);
            }
        }

        public void RestartTimer(object tickHandle)
        {
            if (tickHandle is DispatcherTimer)
            {
                ((DispatcherTimer)tickHandle).Start();
            }
            else if (tickHandle is Task<DispatcherTimer>)
            {
                var asyncItem = _uiDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    var timer = await (Task<DispatcherTimer>)tickHandle;
                    timer.Start();
                });
            }
            else if (tickHandle is ThreadPoolTimer)
            {
                throw new NotImplementedException();
            }
        }


        public void StartThreadPoolTimer(Func<object, Task> action, TimeSpan timer)
        {
            ThreadPoolTimer.CreateTimer(async (obj) => await action(obj), timer);
        }
    }
}
