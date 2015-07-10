using GalaSoft.MvvmLight;
using Newtonsoft.Json;
using SnooStream.Common;
using SnooStream.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public class NavigationStateUtility
    {
		Dictionary<string, ViewModelBase> _navState;
		Dictionary<string, int> _navDuplicates;
		Stack<string> _navStateInsertionOrder;
        SnooStreamViewModel _rootContext;
        public NavigationStateUtility(string existingState, SnooStreamViewModel rootContext)
        {
            _rootContext = rootContext;
            _navDuplicates = new Dictionary<string, int>();
			_navState = new Dictionary<string, ViewModelBase>();
			_navStateInsertionOrder = new Stack<string>();
            if (!string.IsNullOrEmpty(existingState))
            {
                var serializedItems = JsonConvert.DeserializeObject<Tuple<IEnumerable<string>, Dictionary<string, string>>>(existingState);
				ViewModelBase context = rootContext;
                foreach (var item in serializedItems.Item1.Reverse())
                {
					if (!_navState.ContainsKey(item))
					{
						context = RestoreStateItem(serializedItems.Item2[item], context) as ViewModelBase;
					}
					else
					{
						context = _navState[item];
					}
					AddState(context, item);
                }
            }
            
        }

		public static T Remove<T>(Stack<T> stack, T element)
		{
			T obj = stack.Pop();
			if (obj.Equals(element))
			{
				return obj;
			}
			else
			{
				T toReturn = Remove(stack, element);
				stack.Push(obj);
				return toReturn;
			}
		}

        public object AddState(ViewModelBase state, string guid = null)
        {
			if (_navState.ContainsValue(state))
			{
				var navKvp = _navState.FirstOrDefault(kvp => kvp.Value == state);
				guid = navKvp.Key;
				if (_navDuplicates.ContainsKey(guid))
				{
					_navDuplicates[guid] = _navDuplicates[guid] + 1;
				}
				else
					_navDuplicates[guid] = 2;
			}
			else if (guid != null)
			{
				_navState.Add(guid, state);
			}
			else
			{
				guid = Uri.EscapeDataString(Guid.NewGuid().ToString());
				_navState.Add(guid, state);
			}

			_navStateInsertionOrder.Push(guid);
            return guid;
        }

        public void RemoveState(string guid)
        {
			if (_navDuplicates.ContainsKey(guid))
			{
				_navDuplicates[guid] = _navDuplicates[guid] - 1;
				if (_navDuplicates[guid] <= 0)
				{
					_navState.Remove(guid);
					_navDuplicates.Remove(guid);
				}
			}
			else
			{
				_navState.Remove(guid);
			}
			Remove(_navStateInsertionOrder, guid);
        }

        public string DumpState()
        {
			var dictionary = new Dictionary<string, string>();

			foreach (var kvp in _navState)
			{
				if (!dictionary.ContainsKey(kvp.Key))
					dictionary.Add(kvp.Key, DumpStateItem(kvp.Value));
			}
            return JsonConvert.SerializeObject(Tuple.Create((IEnumerable<string>)_navStateInsertionOrder, dictionary));
        }

        private string DumpStateItem(object state)
        {
            return ViewModelDumpUtility.Dump(state as ViewModelBase);
        }

		private ViewModelBase RestoreStateItem(string state, ViewModelBase context)
        {
			return ViewModelDumpUtility.LoadFromDump(state, context, _rootContext);
        }

        public ViewModelBase this[string guid]
        {
            get
            {
                return _navState[guid];
            }
        }

		public static ViewModelBase GetDataContext(string stateGuid)
        {
			return SnooStreamViewModel.NavigationService.GetState(stateGuid);
        }

        public void ValidateParameters(HashSet<string> validParameters)
        {
            foreach (var item in _navState.Keys.ToList())
            {
                if (!validParameters.Contains(item))
                {
                    RemoveState(item);
                }
            }
        }
    }
}
