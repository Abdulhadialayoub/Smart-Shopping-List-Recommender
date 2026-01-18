using System.Text.RegularExpressions;
using System.Web;

namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Service for sanitizing AI-generated outputs to prevent XSS and other security issues
/// </summary>
public class OutputSanitizationService : IOutputSanitizationService
{
    private readonly ILogger<OutputSanitizationService> _logger;
    
    // Regex to detect potentially harmful patterns
    private static readonly Regex ScriptTagRegex = new(@"<script[^>]*>.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HtmlTagRegex = new(@"<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex JavaScriptProtocolRegex = new(@"javascript:", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DataProtocolRegex = new(@"data:", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    // Allowed URL protocols
    private static readonly HashSet<string> AllowedProtocols = new(StringComparer.OrdinalIgnoreCase)
    {
        "http", "https"
    };

    public OutputSanitizationService(ILogger<OutputSanitizationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Sanitizes a verified recipe response
    /// Requirements: 3.4 - Verify product names are specific and searchable
    /// Requirements: 6.4 - Include product name, price, merchant, and product URL
    /// </summary>
    public VerifiedRecipeResponse SanitizeRecipeResponse(VerifiedRecipeResponse response)
    {
        if (response == null) return response;

        response.RecipeName = SanitizeText(response.RecipeName);
        response.PrepTime = SanitizeText(response.PrepTime);
        response.CookTime = SanitizeText(response.CookTime);

        if (response.Ingredients != null)
        {
            foreach (var ingredient in response.Ingredients)
            {
                ingredient.Name = SanitizeText(ingredient.Name);
                ingredient.Quantity = SanitizeText(ingredient.Quantity);
                ingredient.Unit = SanitizeText(ingredient.Unit);
            }
        }

        if (response.MissingIngredients != null)
        {
            foreach (var ingredient in response.MissingIngredients)
            {
                ingredient.Name = SanitizeText(ingredient.Name);
                ingredient.Quantity = SanitizeText(ingredient.Quantity);
                ingredient.Unit = SanitizeText(ingredient.Unit);
            }
        }

        if (response.Steps != null)
        {
            for (int i = 0; i < response.Steps.Count; i++)
            {
                response.Steps[i] = SanitizeText(response.Steps[i]);
            }
        }

        if (response.Metadata?.Corrections != null)
        {
            for (int i = 0; i < response.Metadata.Corrections.Count; i++)
            {
                response.Metadata.Corrections[i] = SanitizeText(response.Metadata.Corrections[i]);
            }
        }

        return response;
    }

    /// <summary>
    /// Sanitizes a verified product response
    /// Requirements: 3.4 - Verify product names are specific and searchable
    /// </summary>
    public VerifiedProductResponse SanitizeProductResponse(VerifiedProductResponse response)
    {
        if (response == null) return response;

        if (response.Recommendations != null)
        {
            foreach (var recommendation in response.Recommendations)
            {
                recommendation.ProductName = SanitizeText(recommendation.ProductName);
                recommendation.EstimatedQuantity = SanitizeText(recommendation.EstimatedQuantity);
                recommendation.Reasoning = SanitizeText(recommendation.Reasoning);
            }
        }

        if (response.Metadata?.Corrections != null)
        {
            for (int i = 0; i < response.Metadata.Corrections.Count; i++)
            {
                response.Metadata.Corrections[i] = SanitizeText(response.Metadata.Corrections[i]);
            }
        }

        return response;
    }

    /// <summary>
    /// Sanitizes a verified recipe with prices response
    /// Requirements: 3.4 - Verify product names are specific and searchable
    /// Requirements: 6.4 - Include product name, price, merchant, and product URL
    /// </summary>
    public VerifiedRecipeWithPricesResponse SanitizeRecipeWithPricesResponse(VerifiedRecipeWithPricesResponse response)
    {
        if (response == null) return response;

        // Sanitize the recipe part
        response.Recipe = SanitizeRecipeResponse(response.Recipe);

        // Sanitize product prices
        if (response.ProductPrices != null)
        {
            foreach (var productList in response.ProductPrices.Values)
            {
                foreach (var product in productList)
                {
                    product.Name = SanitizeText(product.Name);
                    product.MerchantName = SanitizeText(product.MerchantName);
                    product.ProductUrl = SanitizeUrl(product.ProductUrl) ?? string.Empty;
                    
                    // Sanitize image URL if present
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        product.ImageUrl = SanitizeUrl(product.ImageUrl) ?? string.Empty;
                    }
                }
            }
        }

        return response;
    }

    /// <summary>
    /// Sanitizes a text string by removing potentially harmful content
    /// - Removes script tags
    /// - Escapes HTML entities
    /// - Removes javascript: and data: protocols
    /// </summary>
    public string SanitizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        // Remove script tags
        text = ScriptTagRegex.Replace(text, string.Empty);

        // Remove javascript: and data: protocols
        text = JavaScriptProtocolRegex.Replace(text, string.Empty);
        text = DataProtocolRegex.Replace(text, string.Empty);

        // Remove HTML tags
        text = HtmlTagRegex.Replace(text, string.Empty);

        // HTML encode to escape any remaining special characters
        text = HttpUtility.HtmlEncode(text);

        // Decode back to normal text (this ensures we don't double-encode)
        text = HttpUtility.HtmlDecode(text);

        return text.Trim();
    }

    /// <summary>
    /// Validates and sanitizes a URL
    /// - Checks for allowed protocols (http, https)
    /// - Removes javascript: and data: protocols
    /// - Returns null if URL is invalid
    /// </summary>
    public string? SanitizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        // Remove any script tags or HTML
        url = ScriptTagRegex.Replace(url, string.Empty);
        url = HtmlTagRegex.Replace(url, string.Empty);

        // Check for javascript: or data: protocols
        if (JavaScriptProtocolRegex.IsMatch(url) || DataProtocolRegex.IsMatch(url))
        {
            _logger.LogWarning("Blocked potentially harmful URL: {Url}", url);
            return null;
        }

        // Try to parse as URI
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            _logger.LogWarning("Invalid URL format: {Url}", url);
            return null;
        }

        // Check if protocol is allowed
        if (!AllowedProtocols.Contains(uri.Scheme))
        {
            _logger.LogWarning("Blocked URL with disallowed protocol: {Protocol}, URL: {Url}", uri.Scheme, url);
            return null;
        }

        // Return the sanitized URL
        return uri.ToString();
    }
}
