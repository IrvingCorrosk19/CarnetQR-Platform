using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CarnetQRPlatform.Application.Services;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
}

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;

    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        try
        {
            if (_cache.TryGetValue(key, out T? value))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return Task.FromResult(value);
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            return Task.FromResult(default(T));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache value for key: {Key}", key);
            return Task.FromResult(default(T));
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var options = new MemoryCacheEntryOptions
            {
                SlidingExpiration = expiration ?? TimeSpan.FromMinutes(30),
                Priority = CacheItemPriority.Normal
            };

            _cache.Set(key, value, options);
            _logger.LogDebug("Cache set for key: {Key}", key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
            return Task.CompletedTask;
        }
    }

    public Task RemoveAsync(string key)
    {
        try
        {
            _cache.Remove(key);
            _logger.LogDebug("Cache removed for key: {Key}", key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache value for key: {Key}", key);
            return Task.CompletedTask;
        }
    }

    public Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            // Para MemoryCache, necesitamos iterar sobre las claves
            // Esto es una implementación simple - en producción podrías usar un mejor approach
            var field = typeof(MemoryCache).GetField("_coherentState", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field?.GetValue(_cache) is object coherentState)
            {
                var entriesCollection = coherentState.GetType().GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
                if (entriesCollection?.GetValue(coherentState) is System.Collections.ICollection entries)
                {
                    var keysToRemove = new List<string>();
                    foreach (var entry in entries)
                    {
                        var entryKey = entry.GetType().GetProperty("Key").GetValue(entry)?.ToString();
                        if (entryKey != null && entryKey.Contains(pattern))
                        {
                            keysToRemove.Add(entryKey);
                        }
                    }

                    foreach (var key in keysToRemove)
                    {
                        _cache.Remove(key);
                    }
                }
            }

            _logger.LogDebug("Cache removed for pattern: {Pattern}", pattern);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache values for pattern: {Pattern}", pattern);
            return Task.CompletedTask;
        }
    }
}
