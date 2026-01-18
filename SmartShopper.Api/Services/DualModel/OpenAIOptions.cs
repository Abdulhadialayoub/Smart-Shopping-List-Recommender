namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Configuration options for OpenAI service.
/// </summary>
public class OpenAIOptions
{
    /// <summary>
    /// OpenAI API key (from environment variable OPENAI_API_KEY)
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model to use for validation (default: gpt-4o-mini for cost efficiency)
    /// </summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Maximum tokens for response
    /// </summary>
    public int MaxTokens { get; set; } = 2000;

    /// <summary>
    /// Temperature for response generation (0.0 = deterministic, 1.0 = creative)
    /// </summary>
    public double Temperature { get; set; } = 0.3;
}
