using SmartShopper.Api.Models;

namespace SmartShopper.Api.Services;

public interface IRecipeService
{
    Task<List<Recipe>> GetRecipeSuggestionsAsync(List<string> availableIngredients, int servings = 2);
    Task<NutritionInfo> GetNutritionInfoAsync(List<string> ingredients);
    Task<Recipe> GenerateRecipeAsync(List<string> ingredients, string? dietaryRestrictions = null);
}