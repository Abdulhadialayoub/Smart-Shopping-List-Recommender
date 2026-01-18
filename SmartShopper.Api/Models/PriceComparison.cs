using System.ComponentModel.DataAnnotations;

namespace SmartShopper.Api.Models;

public class PriceComparison
{
    [MaxLength(100)]
    public string Store { get; set; } = string.Empty;
    
    public double Price { get; set; }
    
    [MaxLength(10)]
    public string Currency { get; set; } = "TL";
    
    public bool IsAvailable { get; set; }
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    [MaxLength(500)]
    public string? ProductUrl { get; set; }
    
    [MaxLength(500)]
    public string? ImageUrl { get; set; }
    
    [MaxLength(100)]
    public string? MerchantName { get; set; }
    
    public double? UnitPrice { get; set; }
    
    /// <summary>
    /// Ürün indirimde mi?
    /// </summary>
    public bool IsOnSale { get; set; }
    
    /// <summary>
    /// İndirim öncesi fiyat (varsa)
    /// </summary>
    public double? OriginalPrice { get; set; }
    
    /// <summary>
    /// İndirim yüzdesi (varsa)
    /// </summary>
    public int? DiscountPercentage { get; set; }
}
