using Microsoft.Extensions.Caching.Memory;
using System;

namespace MyHorizons.Avalonia.Utility
{
    /// <summary>
    /// Represents a memory cache with caching policy
    /// </summary>
    /// <typeparam name="TItem">Item type to cache</typeparam>
    public class MemoryCache<TItem>
    {
        private readonly MemoryCache _memoryCache;

        public MemoryCache(MemoryCacheOptions? memoryCacheOptions = null)
        {
            _memoryCache = new MemoryCache(memoryCacheOptions);
        }

        /// <summary>
        /// Gets or creates a cached item
        /// </summary>
        /// <param name="key">The ID of the item to get or create</param>
        /// <param name="createItem">The method to create the item if it is not cached</param>
        /// <param name="createCacheEntryOptions">Optional method to create memory cache options</param>
        /// <returns></returns>
        public TItem GetOrCreate(object key, Func<TItem> createItem, Func<TItem, MemoryCacheEntryOptions>? createCacheEntryOptions = null)
        {
            if (_memoryCache.TryGetValue(key, out TItem cacheEntry))
                return cacheEntry;

            // Key not in cache, so get data.
            cacheEntry = createItem();

            var cacheEntryOptions = createCacheEntryOptions?.Invoke(cacheEntry);

            // Save data in cache.
            _memoryCache.Set(key, cacheEntry, cacheEntryOptions);
            return cacheEntry;
        }
    }
}
