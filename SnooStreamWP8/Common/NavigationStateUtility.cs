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

namespace SnooStreamWP8.Common
{
    class NavigationStateUtility
    {
		Dictionary<string, ViewModelBase> _navState;
		Stack<string> _navStateInsertionOrder;
        
        public NavigationStateUtility(string existingState)
        {
			_navState = new Dictionary<string, ViewModelBase>();
			_navStateInsertionOrder = new Stack<string>();
            if (!string.IsNullOrEmpty(existingState))
            {
                var serializedItems = JsonConvert.DeserializeObject<Tuple<IEnumerable<string>, Dictionary<string, string>>>(existingState);
				ViewModelBase context = null;
                foreach (var item in serializedItems.Item1.Reverse())
                {
					context = RestoreStateItem(serializedItems.Item2[item], context) as ViewModelBase;
					_navState.Add(item, context);
					_navStateInsertionOrder.Push(item);
                }
            }
            
        }

        public string AddState(ViewModelBase state)
        {
            var guid = Uri.EscapeDataString(Guid.NewGuid().ToString());
            _navState.Add(guid, state);
			_navStateInsertionOrder.Push(guid);
            return guid;
        }

        public void RemoveState(string guid)
        {
            _navState.Remove(guid);
			Debug.Assert(_navStateInsertionOrder.Peek() == guid);
			_navStateInsertionOrder.Pop();
        }

        public string DumpState()
        {
            var dictionary = _navState.Select(kvp => new KeyValuePair<string, string>(kvp.Key, DumpStateItem(kvp.Value))).ToDictionary(kvp=> kvp.Key, kvp => kvp.Value);
            return JsonConvert.SerializeObject(Tuple.Create((IEnumerable<string>)_navStateInsertionOrder, dictionary));
        }

        private string DumpStateItem(object state)
        {
            return ViewModelDumpUtility.Dump(state as ViewModelBase);
        }

		private ViewModelBase RestoreStateItem(string state, ViewModelBase context)
        {
			return ViewModelDumpUtility.LoadFromDump(state, context);
        }

        public ViewModelBase this[string guid]
        {
            get
            {
                return _navState[guid];
            }
        }

		public static ViewModelBase GetDataContext(string query, out string stateGuid)
        {
            stateGuid = null;

            if (!string.IsNullOrWhiteSpace(query) && query.StartsWith("state"))
            {
                var splitQuery = query.Split('=');
                stateGuid = splitQuery[1];
                return SnooStreamViewModel.NavigationService.GetState(splitQuery[1]);
            }
            else
                return null;
        }
    }
}
