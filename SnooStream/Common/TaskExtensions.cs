using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public static class TaskExtensions
    {
        public static T TryValue<T>(this Task<T> task) where T : class
        {
            if (task.Status != TaskStatus.RanToCompletion)
                return null;
            else
                return task.Result;
        }

        public static Nullable<T> TryValueS<T>(this Task<T> task) where T : struct
        {
            if (task.Status != TaskStatus.RanToCompletion)
                return null;
            else
                return task.Result;
        }

        public static bool WasSuccessfull(this Task task)
        {
            return task.Status == TaskStatus.RanToCompletion;
        }

		public static bool WasSuccessfull(this Task<Exception> task)
		{
			return task.Status == TaskStatus.RanToCompletion && task.Result == null;
		}
    }
}
