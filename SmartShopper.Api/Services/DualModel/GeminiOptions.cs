namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Configuration options for Gemini API (Validator Model)
/// </summary>
public class GeminiOptions
{
    /// <summary>
    /// Gemini API key for authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for Gemini API
    /// </summary>
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";

    /// <summary>
    /// Model name to use for validation (e.g., "gemini-2.0-flash")
    /// </summary>
    public string Model { get; set; } = "gemini-2.0-flash";

    /// <summary>
    /// Temperature for validation (0.0 to 1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.3;

    /// <summary>
    /// Maximum tokens to generate
    /// </summary>
    public int MaxTokens { get; set; } = 2000;
}
