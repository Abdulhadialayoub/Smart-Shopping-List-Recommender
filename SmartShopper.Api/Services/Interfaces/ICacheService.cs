namespace SmartShopper.Api.Services;

/// <summary>
/// Interface for file-based caching service
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Retrieves cached data by key
    /// </summary>
    Task<T?> GetAsync<T>(string key) where T : class;
    
    /// <summary>
    /// Stores data in cache with optional expiration
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    
    /// <summary>
    /// Cleans old cache files based on age and count limits
    /// </summary>
    Task CleanOldCacheAsync();
    
    /// <summary>
    /// Checks if a cache entry exists and is not expired
    /// </summary>
    Task<bool> ExistsAsync(string key);
}
