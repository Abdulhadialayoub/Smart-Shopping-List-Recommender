using System.Text.Json.Serialization;

namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Comprehensive log of a dual-model verification pipeline execution.
/// Used for debugging, monitoring, and performance analysis.
/// </summary>
public class PipelineExecutionLog
{
    /// <summary>
    /// Unique identifier for this pipeline execution
    /// </summary>
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the pipeline execution started
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// User ID who initiated the request (if available)
    /// </summary>
    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    /// <summary>
    /// User's inventory items provided as input
    /// </summary>
    [JsonPropertyName("userInventory")]
    public List<string> UserInventory { get; set; } = new();

    /// <summary>
    /// Optional recipe type requested by user
    /// </summary>
    [JsonPropertyName("recipeType")]
    public string? RecipeType { get; set; }

    /// <summary>
    /// Shopping list provided for product recommendations
    /// </summary>
    [JsonPropertyName("shoppingList")]
    public List<string>? ShoppingList { get; set; }

    /// <summary>
    /// Type of pipeline execution (Recipe, Product, RecipeWithPrices)
    /// </summary>
    [JsonPropertyName("pipelineType")]
    public string PipelineType { get; set; } = string.Empty;

    /// <summary>
    /// Prompt sent to the generator model
    /// </summary>
    [JsonPropertyName("generatorPrompt")]
    public string GeneratorPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Raw response from the generator model
    /// </summary>
    [JsonPropertyName("generatorResponse")]
    public string GeneratorResponse { get; set; } = string.Empty;

    /// <summary>
    /// Time taken by generator model to respond (milliseconds)
    /// </summary>
    [JsonPropertyName("generatorResponseTimeMs")]
    public int GeneratorResponseTimeMs { get; set; }

    /// <summary>
    /// Prompt sent to the validator model
    /// </summary>
    [JsonPropertyName("validatorPrompt")]
    public string? ValidatorPrompt { get; set; }

    /// <summary>
    /// Raw response from the validator model
    /// </summary>
    [JsonPropertyName("validatorResponse")]
    public string? ValidatorResponse { get; set; }

    /// <summary>
    /// Time taken by validator model to respond (milliseconds)
    /// </summary>
    [JsonPropertyName("validatorResponseTimeMs")]
    public int? ValidatorResponseTimeMs { get; set; }

    /// <summary>
    /// List of corrections made by the validator
    /// </summary>
    [JsonPropertyName("corrections")]
    public List<string> Corrections { get; set; } = new();

    /// <summary>
    /// Whether the pipeline completed successfully
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Error message if pipeline failed
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Total time for the entire pipeline execution (milliseconds)
    /// </summary>
    [JsonPropertyName("totalPipelineTimeMs")]
    public int TotalPipelineTimeMs { get; set; }

    /// <summary>
    /// Whether validation was performed (false if validator failed/skipped)
    /// </summary>
    [JsonPropertyName("wasValidated")]
    public bool WasValidated { get; set; }

    /// <summary>
    /// Number of missing ingredients found (for recipe pipelines)
    /// </summary>
    [JsonPropertyName("missingIngredientsCount")]
    public int? MissingIngredientsCount { get; set; }

    /// <summary>
    /// Time taken for Cimri searches (milliseconds)
    /// </summary>
    [JsonPropertyName("cimriSearchTimeMs")]
    public int? CimriSearchTimeMs { get; set; }

    /// <summary>
    /// Whether the result was served from cache
    /// </summary>
    [JsonPropertyName("cacheHit")]
    public bool CacheHit { get; set; }

    /// <summary>
    /// Additional metadata or notes
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();
}
