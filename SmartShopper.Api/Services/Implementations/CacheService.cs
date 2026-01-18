using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SmartShopper.Api.Services;

/// <summary>
/// File-based cache service for storing scraped data
/// </summary>
public class CacheService : ICacheService
{
    private readonly string _cacheDirectory;
    private readonly TimeSpan _defaultExpiration;
    private readonly int _maxCacheFiles;
    private readonly ILogger<CacheService> _logger;
    private readonly SemaphoreSlim _cleanupLock = new(1, 1);

    public CacheService(IConfiguration configuration, ILogger<CacheService> logger)
    {
        _logger = logger;
        
        // Read configuration
        _cacheDirectory = configuration["CimriScraper:CacheDirectory"] ?? "./cache/cimri";
        _defaultExpiration = TimeSpan.FromMinutes(
            configuration.GetValue<int>("CimriScraper:CacheDurationMinutes", 60));
        _maxCacheFiles = configuration.GetValue<int>("CimriScraper:MaxCacheFiles", 500);
        
        // Ensure cache directory exists
        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
            _logger.LogInformation("Created cache directory: {Directory}", _cacheDirectory);
        }
    }

    /// <summary>
    /// Retrieves cached data by key
    /// </summary>
    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var filePath = GetCacheFilePath(key);
            
            if (!File.Exists(filePath))
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath);
            var cacheEntry = JsonSerializer.Deserialize<CacheEntry<T>>(json);

            if (cacheEntry == null)
            {
                _logger.LogWarning("Failed to deserialize cache entry for key: {Key}", key);
                return null;
            }

            if (cacheEntry.IsExpired())
            {
                _logger.LogDebug("Cache expired for key: {Key}", key);
                File.Delete(filePath);
                return null;
            }

            _logger.LogInformation("Cache hit for key: {Key}", key);
            return cacheEntry.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading cache for key: {Key}", key);
            return null;
        }
    }

    /// <summary>
    /// Stores data in cache with optional expiration
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var filePath = GetCacheFilePath(key);
            var cacheEntry = new CacheEntry<T>
            {
                Content = value,
                Timestamp = DateTime.UtcNow,
                Expiration = expiration ?? _defaultExpiration
            };

            var json = JsonSerializer.Serialize(cacheEntry, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(filePath, json);
            _logger.LogInformation("Cached data for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing cache for key: {Key}", key);
        }
    }

    /// <summary>
    /// Cleans old cache files based on age and count limits
    /// </summary>
    public async Task CleanOldCacheAsync()
    {
        await _cleanupLock.WaitAsync();
        try
        {
            if (!Directory.Exists(_cacheDirectory))
            {
                return;
            }

            var files = Directory.GetFiles(_cacheDirectory, "*.json")
                .Select(f => new FileInfo(f))
                .ToList();

            var deletedCount = 0;

            // Delete expired files (older than expiration time)
            var expiredFiles = files
                .Where(f => DateTime.UtcNow - f.LastWriteTimeUtc > _defaultExpiration)
                .ToList();

            foreach (var file in expiredFiles)
            {
                try
                {
                    file.Delete();
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete expired cache file: {File}", file.Name);
                }
            }

            // If still over limit, delete oldest files
            var remainingFiles = files.Except(expiredFiles).ToList();
            if (remainingFiles.Count > _maxCacheFiles)
            {
                var filesToDelete = remainingFiles
                    .OrderBy(f => f.LastWriteTimeUtc)
                    .Take(remainingFiles.Count - _maxCacheFiles)
                    .ToList();

                foreach (var file in filesToDelete)
                {
                    try
                    {
                        file.Delete();
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete old cache file: {File}", file.Name);
                    }
                }
            }

            if (deletedCount > 0)
            {
                _logger.LogInformation("Cache cleanup completed. Deleted {Count} files", deletedCount);
            }
        }
        finally
        {
            _cleanupLock.Release();
        }
    }

    /// <summary>
    /// Checks if a cache entry exists and is not expired
    /// </summary>
    public async Task<bool> ExistsAsync(string key)
    {
        var data = await GetAsync<object>(key);
        return data != null;
    }

    /// <summary>
    /// Generates cache file path from key using MD5 hash
    /// </summary>
    private string GetCacheFilePath(string key)
    {
        var hash = GenerateMD5Hash(key);
        return Path.Combine(_cacheDirectory, $"{hash}_v1.json");
    }

    /// <summary>
    /// Generates MD5 hash for cache key
    /// </summary>
    private static string GenerateMD5Hash(string input)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

/// <summary>
/// Cache entry wrapper with expiration metadata
/// </summary>
public class CacheEntry<T>
{
    public T Content { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    public TimeSpan Expiration { get; set; }
    
    public bool IsExpired() => DateTime.UtcNow - Timestamp > Expiration;
}
