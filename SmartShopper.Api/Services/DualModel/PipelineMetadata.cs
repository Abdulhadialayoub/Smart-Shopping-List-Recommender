namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Metadata about the dual-model verification pipeline execution.
/// </summary>
public class PipelineMetadata
{
    public string GeneratorModel { get; set; } = "Groq Llama 3";
    public string ValidatorModel { get; set; } = "Gemini 2.0 Flash";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int GeneratorResponseTimeMs { get; set; }
    public int ValidatorResponseTimeMs { get; set; }
    public int TotalPipelineTimeMs { get; set; }
    
    /// <summary>
    /// Indicates whether the validator stage was successfully executed.
    /// </summary>
    public bool WasValidated { get; set; }
    
    /// <summary>
    /// List of corrections made by the validator.
    /// </summary>
    public List<string> Corrections { get; set; } = new();
    
    /// <summary>
    /// Indicates whether the response was served from cache.
    /// </summary>
    public bool CacheHit { get; set; }
    
    /// <summary>
    /// Cache key used for this request (if applicable).
    /// </summary>
    public string? CacheKey { get; set; }
}
