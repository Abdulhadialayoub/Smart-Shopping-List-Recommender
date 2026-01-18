namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Core orchestration service for the dual-model verification pipeline.
/// Coordinates generation (Groq) and validation (Gemini) stages.
/// </summary>
public interface IDualModelVerificationService
{
    /// <summary>
    /// Generates a verified recipe using the dual-model pipeline.
    /// </summary>
    /// <param name="userInventory">List of ingredients available in user's fridge</param>
    /// <param name="recipeType">Optional recipe type preference (e.g., "pasta", "salad")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verified recipe with metadata</returns>
    Task<VerifiedRecipeResponse> GenerateVerifiedRecipeAsync(
        List<string> userInventory,
        string? recipeType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates verified product recommendations using the dual-model pipeline.
    /// </summary>
    /// <param name="shoppingList">List of items user wants to purchase</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verified product recommendations with metadata</returns>
    Task<VerifiedProductResponse> GenerateVerifiedProductRecommendationsAsync(
        List<string> shoppingList,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a verified recipe with Cimri price lookups for missing ingredients.
    /// </summary>
    /// <param name="userInventory">List of ingredients available in user's fridge</param>
    /// <param name="recipeType">Optional recipe type preference</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verified recipe with product prices from Cimri</returns>
    Task<VerifiedRecipeWithPricesResponse> GenerateVerifiedRecipeWithPricesAsync(
        List<string> userInventory,
        string? recipeType = null,
        CancellationToken cancellationToken = default);
}
