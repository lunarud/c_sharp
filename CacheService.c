using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

public class CacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<CacheService> _logger;
    private readonly TimeSpan _defaultCacheDuration = TimeSpan.FromMinutes(5);

    public CacheService(IMemoryCache memoryCache, ILogger<CacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    /// <summary>
    /// Wrapper method around GetOrCreateAsync to simplify usage and add logging.
    /// </summary>
    /// <typeparam name="T">The type of the cache value.</typeparam>
    /// <param name="key">Cache key to look up.</param>
    /// <param name="factory">Factory method to retrieve the value if it's not in the cache.</param>
    /// <param name="absoluteExpiration">Optional cache expiration time.</param>
    /// <returns>The value from the cache or the result of the factory method.</returns>
    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null)
    {
        if (_memoryCache.TryGetValue(key, out T cacheValue))
        {
            _logger.LogInformation($"Cache hit for key: {key}");
            return cacheValue;
        }

        _logger.LogInformation($"Cache miss for key: {key}. Fetching new data.");

        cacheValue = await _memoryCache.GetOrCreateAsync(key, async cacheEntry =>
        {
            cacheEntry.AbsoluteExpirationRelativeToNow = absoluteExpiration ?? _defaultCacheDuration;
            cacheEntry.Priority = CacheItemPriority.Normal;

            T value = await factory();
            return value;
        });

        _logger.LogInformation($"Cache updated for key: {key}");
        return cacheValue;
    }

    /// <summary>
    /// Example method that fetches and caches data.
    /// </summary>
    /// <returns></returns>
    public async Task<object> GetCachedDataAsync()
    {
        return await GetOrCreateAsync("MyData", async () =>
        {
            // Simulate fetching data from a database or API
            await Task.Delay(1000); // Simulate some delay
            return new { Data = "Fetched data", Time = DateTime.UtcNow };
        });
    }
}
