namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Configuration options for the Dual-Model Verification system
/// </summary>
public class DualModelVerificationOptions
{
    /// <summary>
    /// Timeout in seconds for the generator model (Groq)
    /// </summary>
    public int GeneratorTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Timeout in seconds for the validator model (Gemini)
    /// </summary>
    public int ValidatorTimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// Total timeout in seconds for the recipe generation pipeline
    /// </summary>
    public int RecipePipelineTimeoutSeconds { get; set; } = 20;

    /// <summary>
    /// Total timeout in seconds for the product recommendation pipeline
    /// </summary>
    public int ProductPipelineTimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// Whether caching is enabled for validated responses
    /// </summary>
    public bool CacheEnabled { get; set; } = true;

    /// <summary>
    /// Time-to-live in hours for validation cache entries
    /// </summary>
    public int ValidationCacheTtlHours { get; set; } = 1;

    /// <summary>
    /// Time-to-live in hours for Cimri search cache entries
    /// </summary>
    public int CimriCacheTtlHours { get; set; } = 24;

    /// <summary>
    /// Maximum number of items allowed in user inventory
    /// </summary>
    public int MaxUserInventorySize { get; set; } = 100;

    /// <summary>
    /// Maximum number of items allowed in shopping list
    /// </summary>
    public int MaxShoppingListSize { get; set; } = 50;

    /// <summary>
    /// Maximum number of Cimri results to return per product
    /// </summary>
    public int MaxCimriResultsPerProduct { get; set; } = 3;

    /// <summary>
    /// Whether to enable parallel execution of Cimri searches
    /// </summary>
    public bool EnableParallelCimriSearch { get; set; } = true;
}
