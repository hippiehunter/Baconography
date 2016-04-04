using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public class ProgressLazy<T>
    {
        private Lazy<Task<T>> _innerLazy;
        List<Tuple<IProgress<float>, CancellationToken, TaskCompletionSource<T>>> _watchers;
        CancellationTokenSource _innerCancelToken;
        Progress<float> _innerProgress;
        Func<IProgress<float>, CancellationToken, Task<T>> _impl;
        public ProgressLazy(Func<IProgress<float>, CancellationToken, Task<T>> impl)
        {
            _impl = impl;
            _innerCancelToken = new CancellationTokenSource();
            _innerProgress = new Progress<float>();
            _watchers = new List<Tuple<IProgress<float>, CancellationToken, TaskCompletionSource<T>>>();
            _innerLazy = new Lazy<Task<T>>(Impl);
            _innerProgress.ProgressChanged += ProgressChanged;
        }

        private Task<T> Impl()
        {
            return _impl(_innerProgress, _innerCancelToken.Token);
        }

        private void ProgressChanged(object sender, float e)
        {
            lock (this)
            {
                foreach (var tpl in _watchers)
                {
                    tpl.Item1.Report(e);
                }
            }
        }

        public Task<T> Result(IProgress<float> progress, CancellationToken token)
        {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
            var tpl = Tuple.Create(progress, token, tcs);

            lock(this)
                _watchers.Add(tpl);

            token.Register(Canceled, tpl);
            _innerLazy.Value.ContinueWith(tsk =>
            {
                if (tsk.IsCanceled)
                    tcs.TrySetCanceled();
                else if (tsk.Exception != null)
                    tcs.TrySetException(tsk.Exception);
                else if (tsk.IsFaulted)
                    tcs.TrySetException(new Exception());
                else
                    tcs.TrySetResult(tsk.Result);
            });
            return tcs.Task;
        }

        private void Canceled(object state)
        {
            var realState = state as Tuple<IProgress<float>, CancellationToken, TaskCompletionSource<T>>;
            if (realState != null)
            {
                realState.Item3.TrySetCanceled();
                lock (this)
                {
                    _watchers.Remove(realState);
                    if (_watchers.Count == 0)
                    {
                        _innerCancelToken.Cancel();
                        _innerCancelToken = new CancellationTokenSource();
                        _innerLazy = new Lazy<Task<T>>(Impl);
                    }
                }
            }
        }
    }
}
