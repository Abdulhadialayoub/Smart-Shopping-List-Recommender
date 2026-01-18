using SmartShopper.Api.Models;
using System.Collections.Concurrent;

namespace SmartShopper.Api.Services;

public class PriceCacheService
{
    private readonly ConcurrentDictionary<string, CachedPriceData> _cache = new();
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30); // 30 dakika cache

    public bool TryGetCachedPrices(string productName, out List<PriceComparison> prices)
    {
        prices = new List<PriceComparison>();
        
        var key = productName.ToLowerInvariant();
        
        if (_cache.TryGetValue(key, out var cachedData))
        {
            if (DateTime.UtcNow - cachedData.CachedAt < _cacheExpiry)
            {
                prices = cachedData.Prices;
                return true;
            }
            else
            {
                // Süresi dolmuş cache'i temizle
                _cache.TryRemove(key, out _);
            }
        }
        
        return false;
    }

    public void CachePrices(string productName, List<PriceComparison> prices)
    {
        var key = productName.ToLowerInvariant();
        var cachedData = new CachedPriceData
        {
            Prices = prices,
            CachedAt = DateTime.UtcNow
        };
        
        _cache.AddOrUpdate(key, cachedData, (k, v) => cachedData);
    }

    public void ClearExpiredCache()
    {
        var expiredKeys = _cache
            .Where(kvp => DateTime.UtcNow - kvp.Value.CachedAt >= _cacheExpiry)
            .Select(kvp => kvp.Key)
            .ToList();
        
        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }
    }

    public int GetCacheCount() => _cache.Count;

    private class CachedPriceData
    {
        public List<PriceComparison> Prices { get; set; } = new();
        public DateTime CachedAt { get; set; }
    }
}