using System.ComponentModel.DataAnnotations;

namespace SmartShopper.Api.Models;

public class User
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? TelegramChatId { get; set; }
    
    [MaxLength(100)]
    public string? TelegramUsername { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}