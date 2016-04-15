using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public class WeakListener<T>
    {
        List<WeakReference<Action<T>>> _refreshRegistration = new List<WeakReference<Action<T>>>();

        public void AddListener(Action<T> listener)
        {
            _refreshRegistration.Add(new WeakReference<Action<T>>(listener));
        }

        public void TriggerListeners(T parameter)
        {
            var removalList = new List<WeakReference<Action<T>>>();
            foreach (var weakListener in _refreshRegistration)
            {
                Action<T> listener;
                if (weakListener.TryGetTarget(out listener))
                {
                    listener(parameter);
                }
                else
                {
                    removalList.Add(weakListener);
                }
            }
            foreach (var removalTarget in removalList)
                _refreshRegistration.Remove(removalTarget);
        }
    }
}
