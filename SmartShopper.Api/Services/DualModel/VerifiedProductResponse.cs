namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Response model for verified product recommendations.
/// </summary>
public class VerifiedProductResponse
{
    public List<ProductRecommendation> Recommendations { get; set; } = new();
    
    /// <summary>
    /// Metadata about the pipeline execution.
    /// </summary>
    public PipelineMetadata Metadata { get; set; } = new();
}
