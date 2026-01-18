namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Configuration options for Groq API (Generator Model)
/// </summary>
public class GroqOptions
{
    /// <summary>
    /// Groq API key for authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for Groq API
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";

    /// <summary>
    /// Model name to use for generation (e.g., "llama-3.1-8b-instant")
    /// </summary>
    public string Model { get; set; } = "llama-3.1-8b-instant";

    /// <summary>
    /// Temperature for generation (0.0 to 1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Maximum tokens to generate
    /// </summary>
    public int MaxTokens { get; set; } = 2000;
}
