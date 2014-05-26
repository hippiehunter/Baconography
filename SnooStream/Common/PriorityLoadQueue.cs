using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public enum LoadContextType
    {
        Minor = 0,
        Major,
        Primary,
        Immediate
    }

    public enum EstimatedLoadCost
    {
        Light,
        Medium,
        Heavy
    }

    public class PriorityLoadQueue
    {
        public static Func<Task> QueueHelper(string context, LoadContextType contextType, Func<Task> operation)
        {
            return () => SnooStreamViewModel.LoadQueue.QueueLoadItem(context, contextType, operation);
        }

        public static Func<Task> QueueHelper(Func<Task> operation)
        {
            return () => SnooStreamViewModel.LoadQueue.QueueLoadItem("", LoadContextType.Immediate, operation);
        }

		public static Func<Action<int>, Task> QueueHelper (Func<Action<int>, Task> operation)
		{
			return (report) => SnooStreamViewModel.LoadQueue.QueueLoadItem("", LoadContextType.Immediate, () => operation(report));
		}

        private class LoadItem
        {
            public LoadContextType ContextType;
            public EstimatedLoadCost Cost;
            public Func<Task> Operation;
            public TaskCompletionSource<bool> CompletionSource;
        }

        CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();
        bool _isDraining = false;
        Dictionary<string, List<LoadItem>> _loadItems = new Dictionary<string, List<LoadItem>>();
        List<LoadItem> _immediateLoadItems = new List<LoadItem>();
        List<string> _minorContexts = new List<string>();
        string _majorContext;
        string _primaryContext;

        public Task QueueLoadItem(string loadContext, LoadContextType contexType, Func<Task> operation)
        {
            var loadItem = new LoadItem { ContextType = contexType, Cost = EstimatedLoadCost.Medium, Operation = operation, CompletionSource = new TaskCompletionSource<bool>() };

            //if we're not draining load items, start it with our current item, otherwise put us in the list
            lock (this)
            {
                if (contexType == LoadContextType.Immediate)
                {
                    _immediateLoadItems.Add(loadItem);
                }
				else if (_loadItems.ContainsKey(loadContext))
                {
                    var targetList = _loadItems[loadContext];
                    targetList.Add(loadItem);
                    targetList.Sort((x, y) => ((int)x.ContextType).CompareTo((int)y.ContextType));
                }
                else
                {
                    _loadItems.Add(loadContext, new List<LoadItem> { loadItem });
                }
                
                if(!_isDraining)
                {
                    _isDraining = true;
                    Task.Run(() => DrainQueue());
                }
            }
            return loadItem.CompletionSource.Task;
        }

        public void SetMajorContext(string loadContext)
        {
			if (_majorContext != loadContext)
			{
				_majorContext = loadContext;
				lock (this)
					_minorContexts.Clear();
			}
        }

        public void SetMinorContext(string loadContext)
        {
            lock (this)
                _minorContexts.Add(loadContext);
        }

        public void SetPrimaryLoadContext(string loadContext)
        {
            _primaryContext = loadContext;
        }

        LoadItem PopOneFromContext(string context)
        {
            lock (this)
            {
                if (_immediateLoadItems.Count > 0)
                {
                    var result = _immediateLoadItems[0];
                    _immediateLoadItems.RemoveAt(0);
                    return result;
                }

                List<LoadItem> items;
                if(context == null)
                {
                    foreach(var itemTpl in _loadItems)
                    {
                        if(itemTpl.Value.Count > 0)
                        { 
                            var result = itemTpl.Value[0];
                            itemTpl.Value.RemoveAt(0);
                            return result;
                        }
                    }
                }
                else if (_loadItems.TryGetValue(context, out items))
                {
                    if(items.Count > 0)
                    {
                        var result = items[0];
                        items.RemoveAt(0);
                        return result;
                    }
                }
            }
            return null;
        }

        LoadItem GetNextLoadItem()
        {
            //enumerate over the loadItems and pick the one that most closely matches our load context
            LoadItem nextItem = null;
            if (_primaryContext != null)
                nextItem = PopOneFromContext(_primaryContext);
            
            if (_majorContext != null && nextItem == null)
                nextItem = PopOneFromContext(_majorContext);
            
            
            if (_minorContexts.Count > 0 && nextItem == null)
            {
                foreach (var item in _minorContexts)
                {
                    nextItem = PopOneFromContext(item);
                    if (nextItem != null)
                        break;
                }
            }

            if (nextItem == null)
            {
                nextItem = PopOneFromContext(null);
            }

            if (nextItem != null && nextItem.CompletionSource.Task.Status != TaskStatus.WaitingToRun &&
                nextItem.CompletionSource.Task.Status != TaskStatus.WaitingForActivation)
                return GetNextLoadItem();
            else
                return nextItem;
        }

		IEnumerable<LoadItem> LoadItemStream()
		{
			for (; ; )
			{
				if (_cancelTokenSource.IsCancellationRequested)
					yield break;
				else
				{
					var nextLoadItem = GetNextLoadItem();
					if (nextLoadItem != null)
						yield return nextLoadItem;
					else
						yield break;
				}
			}
		}

		public int LoadConcurrency { get; set; }
		public int LoadTimout { get; set; }

        void DrainQueue()
        {
			LoadConcurrency = 4;
			LoadTimout = 15000;
            try
            {
				var currentTasks = new Dictionary<Task, DateTime>();
				foreach (var currentItem in LoadItemStream())
				{
					currentTasks.Add(ProcLoadItem(currentItem), DateTime.Now);
					if (currentTasks.Count > LoadConcurrency)
					{
						var taskArray = currentTasks.Keys.ToArray();
						var taskIndex = Task.WaitAny(taskArray, LoadTimout, _cancelTokenSource.Token);
						List<Task> removeTasks = new List<Task>();
						//scrub for overall aged out items and completed items
						var now = DateTime.Now;
						foreach (var taskTpl in currentTasks)
						{
							if (taskTpl.Key.IsCompleted || taskTpl.Key.IsFaulted || taskTpl.Key.IsCanceled)
								removeTasks.Add(taskTpl.Key);
							if ((now - taskTpl.Value).TotalMilliseconds > LoadTimout)
							{
								//let it die elsewhere
								removeTasks.Add(taskTpl.Key);
							}
						}
						foreach (var task in removeTasks)
							currentTasks.Remove(task);
					}
				}
            }
            finally
            {
                _isDraining = false;
            }
        }

		private static async Task ProcLoadItem(LoadItem currentItem)
		{
			try
			{
				await currentItem.Operation();
				currentItem.CompletionSource.TrySetResult(true);
			}
			catch (Exception ex)
			{
				currentItem.CompletionSource.TrySetException(ex);
			}
		}
    }
}
