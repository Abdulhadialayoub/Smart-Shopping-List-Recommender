using SmartShopper.Api.Models;

namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Response model for verified recipe with Cimri price lookups.
/// </summary>
public class VerifiedRecipeWithPricesResponse
{
    public VerifiedRecipeResponse Recipe { get; set; } = new();
    
    /// <summary>
    /// Dictionary mapping ingredient names to Cimri product results.
    /// </summary>
    public Dictionary<string, List<CimriProduct>> ProductPrices { get; set; } = new();
    
    /// <summary>
    /// Metadata about the pipeline execution.
    /// </summary>
    public PipelineMetadata Metadata { get; set; } = new();
}
