using Microsoft.Extensions.Caching.Memory;
using System;

public class CacheService
{
    private readonly IMemoryCache _cache;

    public CacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void SetItemWithEvictionCallback(string key, string value)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5))
            .RegisterPostEvictionCallback(PostEvictionCallback, this);

        _cache.Set(key, value, cacheEntryOptions);
    }

    private void PostEvictionCallback(object key, object value, EvictionReason reason, object state)
    {
        // Logic to handle eviction event
        Console.WriteLine($"Cache item '{key}' was evicted due to {reason}");
    }
}
