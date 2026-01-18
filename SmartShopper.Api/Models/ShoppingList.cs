using System.ComponentModel.DataAnnotations;

namespace SmartShopper.Api.Models;

public class ShoppingList
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public List<ShoppingItem> Items { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsCompleted { get; set; }
    
    public double EstimatedTotal { get; set; }
}

public class ShoppingItem
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string ShoppingListId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public int Quantity { get; set; }
    
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Category { get; set; } = "Diğer";
    
    public bool IsChecked { get; set; }
}
