namespace SmartShopper.Api.Models;

/// <summary>
/// Represents a product from Cimri.com search results
/// </summary>
public class CimriProduct
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? UnitPrice { get; set; }
    public string Quantity { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ProductUrl { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantName { get; set; } = string.Empty;
    
    /// <summary>
    /// Ürün indirimde mi?
    /// </summary>
    public bool IsOnSale { get; set; }
    
    /// <summary>
    /// İndirim öncesi fiyat (varsa)
    /// </summary>
    public decimal? OriginalPrice { get; set; }
    
    /// <summary>
    /// İndirim yüzdesi (varsa)
    /// </summary>
    public int? DiscountPercentage { get; set; }
}

/// <summary>
/// Represents search results from Cimri.com
/// </summary>
public class CimriSearchResult
{
    public List<CimriProduct> Products { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string Query { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents detailed information about a product from Cimri.com
/// </summary>
public class CimriProductDetail
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ProductSpec> Specs { get; set; } = new();
    public List<PriceHistory> PriceHistory { get; set; } = new();
    public List<MarketOffer> Offers { get; set; } = new();
}

/// <summary>
/// Represents a group of product specifications
/// </summary>
public class ProductSpec
{
    public string Group { get; set; } = string.Empty;
    public List<SpecItem> Items { get; set; } = new();
}

/// <summary>
/// Represents a single specification item
/// </summary>
public class SpecItem
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Represents a price history entry for a product
/// </summary>
public class PriceHistory
{
    public DateTime Date { get; set; }
    public decimal Price { get; set; }
}

/// <summary>
/// Represents a market offer for a product
/// </summary>
public class MarketOffer
{
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? UnitPrice { get; set; }
}

/// <summary>
/// Generic cache entry with expiration support
/// </summary>
/// <typeparam name="T">Type of cached content</typeparam>
public class CacheEntry<T>
{
    public T Content { get; set; } = default!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public TimeSpan Expiration { get; set; }
    
    /// <summary>
    /// Checks if the cache entry has expired
    /// </summary>
    /// <returns>True if expired, false otherwise</returns>
    public bool IsExpired() => DateTime.UtcNow - Timestamp > Expiration;
}
