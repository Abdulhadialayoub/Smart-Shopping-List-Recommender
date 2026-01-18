using SmartShopper.Api.Services;

namespace SmartShopper.Api.Models;

public class Recipe
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Ingredients { get; set; } = new();
    public List<string> Instructions { get; set; } = new();
    public int PrepTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public int Servings { get; set; }
    public string Difficulty { get; set; } = string.Empty;
    public NutritionInfo? Nutrition { get; set; }
    public List<string> AvailableIngredients { get; set; } = new();
    public List<string> MissingIngredients { get; set; } = new();
    public double MatchPercentage { get; set; }
    public string? AiComment { get; set; } // Gemini AI yorumu
    public DateTime? CommentGeneratedAt { get; set; }
}