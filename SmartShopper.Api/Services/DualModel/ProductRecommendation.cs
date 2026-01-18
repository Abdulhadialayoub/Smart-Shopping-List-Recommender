namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Represents a product recommendation with quantity and reasoning.
/// </summary>
public class ProductRecommendation
{
    public string ProductName { get; set; } = string.Empty;
    public string EstimatedQuantity { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
}
