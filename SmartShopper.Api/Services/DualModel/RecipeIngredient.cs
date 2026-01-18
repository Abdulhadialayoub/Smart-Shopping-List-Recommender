namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Represents an ingredient in a recipe with quantity and unit.
/// </summary>
public class RecipeIngredient
{
    public string Name { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
}
