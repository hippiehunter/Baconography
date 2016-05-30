using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace SnooStream.Common
{
    class LazyTemplate
    {
        protected static Dictionary<Type, ResourceDictionary> _loadedRDs = new Dictionary<Type, ResourceDictionary>();
        protected Lazy<DataTemplate> _value;
        public DataTemplate Value { get { return _value.Value; } }
    }

    class LazyTemplate<T> : LazyTemplate where T : ResourceDictionary, new() 
    {
        public LazyTemplate(string targetKey)
        {
            _value = new Lazy<DataTemplate>(() =>
            {
                ResourceDictionary targetRD;
                if(!_loadedRDs.TryGetValue(typeof(T), out targetRD))
                {
                    targetRD = new T();
                }

                return targetRD[targetKey] as DataTemplate;
            });
        }


    }
}
