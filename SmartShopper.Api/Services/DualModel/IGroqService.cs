namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Service for fast generation using Groq (Llama 3).
/// Handles initial draft generation for recipes and product recommendations.
/// </summary>
public interface IGroqService
{
    /// <summary>
    /// Generates a draft recipe using Groq's fast generation capabilities.
    /// </summary>
    /// <param name="userInventory">List of ingredients available in user's fridge</param>
    /// <param name="recipeType">Optional recipe type preference</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON string containing draft recipe</returns>
    Task<string> GenerateRecipeDraftAsync(
        List<string> userInventory,
        string? recipeType,
        CancellationToken cancellationToken);

    /// <summary>
    /// Generates draft product recommendations using Groq.
    /// </summary>
    /// <param name="shoppingList">List of items user wants to purchase</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON string containing draft product recommendations</returns>
    Task<string> GenerateProductRecommendationsAsync(
        List<string> shoppingList,
        CancellationToken cancellationToken);
}
