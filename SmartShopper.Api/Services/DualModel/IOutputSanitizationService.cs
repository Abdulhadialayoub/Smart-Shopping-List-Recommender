namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Service for sanitizing AI-generated outputs
/// </summary>
public interface IOutputSanitizationService
{
    /// <summary>
    /// Sanitizes a verified recipe response
    /// Requirements: 3.4 - Verify product names are specific and searchable
    /// Requirements: 6.4 - Include product name, price, merchant, and product URL
    /// </summary>
    VerifiedRecipeResponse SanitizeRecipeResponse(VerifiedRecipeResponse response);

    /// <summary>
    /// Sanitizes a verified product response
    /// Requirements: 3.4 - Verify product names are specific and searchable
    /// </summary>
    VerifiedProductResponse SanitizeProductResponse(VerifiedProductResponse response);

    /// <summary>
    /// Sanitizes a verified recipe with prices response
    /// Requirements: 3.4 - Verify product names are specific and searchable
    /// Requirements: 6.4 - Include product name, price, merchant, and product URL
    /// </summary>
    VerifiedRecipeWithPricesResponse SanitizeRecipeWithPricesResponse(VerifiedRecipeWithPricesResponse response);

    /// <summary>
    /// Sanitizes a text string by removing potentially harmful content
    /// </summary>
    string SanitizeText(string text);

    /// <summary>
    /// Validates and sanitizes a URL
    /// </summary>
    string? SanitizeUrl(string? url);
}
