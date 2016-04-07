using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    //Hook this up on App creation and it will run tasks periodically, and force a run if the app is suspending
    public class PeriodicTask
    {
        public static PeriodicTask DefaultTask { get; set; }

        List<Tuple<Func<bool>, Func<Task>>> _tasks = new List<Tuple<Func<bool>, Func<Task>>>();
        CancellationTokenSource _cancelSource = new CancellationTokenSource();
        Task _currentTask;
        int _duration;
        public PeriodicTask(int duration)
        {
            _duration = duration;
        }

        public void AddTask(Func<bool> hasChanges, Func<Task> task)
        {
            _tasks.Add(Tuple.Create(hasChanges, task));
        }


        public async Task Suspend()
        {
            _cancelSource.Cancel();
            _cancelSource = new CancellationTokenSource();
            if (_currentTask != null)
                await _currentTask;

            foreach (var task in _tasks)
            {
                try
                {
                    if (task.Item1())
                    {
                        await task.Item2();
                    }
                }
                catch (Exception)
                {
#if DEBUG
                    throw;
#endif
                }
            }
        }

        public void Run()
        {
            Task.Run(RunImpl);
        }

        private async Task RunImpl()
        {
            try
            {
                var cancelToken = _cancelSource.Token;
                while (!cancelToken.IsCancellationRequested)
                {
                    foreach (var task in _tasks)
                    {
                        try
                        {
                            if (task.Item1())
                            {
                                _currentTask = task.Item2();
                                await _currentTask;
                                _currentTask = null;
                                //give back to the threadpool to reschedule so we dont occupy a full thread for an extended period
                                await Task.Yield();
                            }
                        }
                        catch (Exception)
                        {
#if DEBUG
                            throw;
#endif
                        }
                    }
                    await Task.Delay(_duration);
                }
            }
            catch (Exception)
            {
#if DEBUG
                throw;
#endif
            }
        }
    }
}
