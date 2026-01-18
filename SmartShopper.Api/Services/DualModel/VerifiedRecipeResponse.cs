namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Response model for verified recipe generation.
/// </summary>
public class VerifiedRecipeResponse
{
    public string RecipeName { get; set; } = string.Empty;
    public List<RecipeIngredient> Ingredients { get; set; } = new();
    public List<RecipeIngredient> MissingIngredients { get; set; } = new();
    public List<string> Steps { get; set; } = new();
    public string PrepTime { get; set; } = string.Empty;
    public string CookTime { get; set; } = string.Empty;
    public int Servings { get; set; }
    
    /// <summary>
    /// Metadata about the pipeline execution.
    /// </summary>
    public PipelineMetadata Metadata { get; set; } = new();
}
