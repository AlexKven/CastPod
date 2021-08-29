using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Utilities
{
    public class MemoryCache<TKey, TValue>
    {
        private TimeSpan? TimeToLive { get; }
        private Dictionary<TKey, (DateTime, TValue)> CacheStore { get; }
            = new Dictionary<TKey, (DateTime, TValue)>();

        private readonly object LockObj = new object();

        public MemoryCache(TimeSpan? timeToLive)
        {
            TimeToLive = timeToLive;
        }

        public MemoryCache() { }

        public bool TryGet(TKey key, out TValue value)
        {
            value = default;
            var now = DateTime.Now;
            lock (LockObj)
            {
                if (CacheStore.TryGetValue(key, out var cached))
                {
                    if (cached.Item1 > now)
                    {
                        value = cached.Item2;
                        return true;
                    }
                    else
                    {
                        CacheStore.Remove(key);
                    }
                }
            }
            return false;
        }

        private DateTime GetExpiration(DateTime now)
        {
            if (TimeToLive.HasValue)
            {
                return now + TimeToLive.Value;
            }
            else
            {
                return DateTime.MaxValue;
            }
        }

        public async Task<TValue> GetAsync(TKey key, Func<Task<TValue>> fallback)
        {
            var now = DateTime.Now;
            if (TryGet(key, out var result))
                return result;
            result = await fallback();
            lock (LockObj)
            {
                CacheStore[key] = (GetExpiration(now), result);
            }
            return result;
        }

        public TValue Get(TKey key, Func<TValue> fallback)
        {
            var now = DateTime.Now;
            if (TryGet(key, out var result))
                return result;
            result = fallback();
            lock (LockObj)
            {
                CacheStore[key] = (GetExpiration(now), result);
            }
            return result;
        }

        public void Clear()
        {

            lock (LockObj)
            {
                CacheStore.Clear();
            }
        }
    }
}
