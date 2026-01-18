using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartShopper.Api.Models;

/// <summary>
/// Buzdolabındaki bir öğeyi temsil eder
/// </summary>
public class FridgeItem
{
    /// <summary>Öğe ID'si</summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>Kullanıcı ID'si</summary>
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>Ürün adı</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>Ürün kategorisi</summary>
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;
    
    /// <summary>Miktar</summary>
    public int Quantity { get; set; }
    
    /// <summary>Birim (adet, kg, litre vb.)</summary>
    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;
    
    /// <summary>Son kullanma tarihi</summary>
    public DateTime ExpiryDate { get; set; }
    
    /// <summary>Eklenme tarihi</summary>
    public DateTime AddedDate { get; set; } = DateTime.UtcNow;
    
    /// <summary>Ürün fotoğrafı URL (local storage)</summary>
    [MaxLength(500)]
    public string? ImageUrl { get; set; }
    
    /// <summary>Süresi geçmiş mi?</summary>
    [NotMapped]
    public bool IsExpired => DateTime.UtcNow > ExpiryDate;
    
    /// <summary>Son kullanma tarihine kaç gün kaldı</summary>
    [NotMapped]
    public int DaysUntilExpiry => (ExpiryDate - DateTime.UtcNow).Days;
}
