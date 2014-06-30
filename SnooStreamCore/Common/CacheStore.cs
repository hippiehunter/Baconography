using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooStream.Common
{
    public class CacheStore<T>
    {
        /// <summary>
        /// In-memory cache dictionary
        /// </summary>
        private Dictionary<string, T> _cache;
        private object _sync;


        /// <summary>
        /// Cache initializer
        /// </summary>
        public CacheStore()
        {
            _cache = new Dictionary<string, T>();
            _sync = new object();
        }
        
        /// <summary>
        /// Get an object from cache
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="key">Name of key in cache</param>
        /// <returns>Object from cache</returns>
        public T GetOrCreate(string key, Func<T> creator)
        {
            lock (_sync)
            {
                if (_cache.ContainsKey(key) == false)
                    _cache.Add(key, creator());
                
                return _cache[key];
            }
        }

        public void Add(string key, T value)
        {
            lock (_sync)
            {
                if (!_cache.ContainsKey(key))
                {
                    _cache.Add(key, value);
                }
            }
        }

        public IEnumerable<KeyValuePair<string, T>> Dump()
        {
            lock (_sync)
            {
                return _cache.ToArray();
            }
        }
    }
}
