using Microsoft.AspNetCore.Mvc;
using SmartShopper.Api.Models;
using SmartShopper.Api.Services;

namespace SmartShopper.Api.Controllers;

/// <summary>
/// Controller for Cimri.com price comparison integration
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CimriController : ControllerBase
{
    private readonly ICimriScraperService _scraperService;
    private readonly ILogger<CimriController> _logger;

    public CimriController(ICimriScraperService scraperService, ILogger<CimriController> logger)
    {
        _scraperService = scraperService;
        _logger = logger;
    }

    /// <summary>
    /// Search for products on Cimri.com
    /// </summary>
    /// <param name="query">Search query (required)</param>
    /// <param name="page">Page number (default: 1, must be > 0)</param>
    /// <param name="sort">Sort parameter (optional)</param>
    /// <returns>Search results with products and pagination info</returns>
    /// <response code="200">Returns the search results</response>
    /// <response code="400">If query is missing or page is invalid</response>
    /// <response code="500">If an error occurs during scraping</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(CimriSearchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchProducts(
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] string sort = "")
    {
        // Validate query parameter
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("Search request received with empty query");
            return BadRequest(new ErrorResponse
            {
                Error = "Query parameter is required",
                Message = "Please provide a search query"
            });
        }

        // Validate page parameter
        if (page <= 0)
        {
            _logger.LogWarning("Search request received with invalid page number: {Page}", page);
            return BadRequest(new ErrorResponse
            {
                Error = "Invalid page number",
                Message = "Page number must be greater than 0"
            });
        }

        try
        {
            _logger.LogInformation("Searching Cimri.com for query: {Query}, page: {Page}, sort: {Sort}", 
                query, page, sort);

            var result = await _scraperService.SearchProductsAsync(query, page, sort);

            _logger.LogInformation("Search completed successfully. Found {Count} products on page {Page} of {TotalPages}", 
                result.Products.Count, result.CurrentPage, result.TotalPages);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching Cimri.com for query: {Query}", query);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Error = "Search failed",
                Message = "An error occurred while searching for products. Please try again later."
            });
        }
    }

    /// <summary>
    /// Get detailed information about a specific product
    /// </summary>
    /// <param name="id">Product ID (required)</param>
    /// <returns>Detailed product information</returns>
    /// <response code="200">Returns the product details</response>
    /// <response code="400">If product ID is missing</response>
    /// <response code="404">If product is not found</response>
    /// <response code="500">If an error occurs during scraping</response>
    [HttpGet("product/{id}")]
    [ProducesResponseType(typeof(CimriProductDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProductDetails(string id)
    {
        // Validate product ID
        if (string.IsNullOrWhiteSpace(id))
        {
            _logger.LogWarning("Product details request received with empty ID");
            return BadRequest(new ErrorResponse
            {
                Error = "Product ID is required",
                Message = "Please provide a valid product ID"
            });
        }

        try
        {
            _logger.LogInformation("Fetching product details for ID: {ProductId}", id);

            var result = await _scraperService.GetProductDetailsAsync(id);

            // Check if product was found (empty name indicates not found)
            if (string.IsNullOrWhiteSpace(result.Name))
            {
                _logger.LogWarning("Product not found for ID: {ProductId}", id);
                return NotFound(new ErrorResponse
                {
                    Error = "Product not found",
                    Message = $"No product found with ID: {id}"
                });
            }

            _logger.LogInformation("Product details fetched successfully for ID: {ProductId}", id);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching product details for ID: {ProductId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Error = "Failed to fetch product details",
                Message = "An error occurred while fetching product details. Please try again later."
            });
        }
    }

    /// <summary>
    /// Test Smart Product Matching - 3 Stage AI-powered product matching
    /// </summary>
    /// <param name="productName">Product name to search (e.g., "Süt", "Peynir")</param>
    /// <param name="quantity">Optional quantity info (e.g., "1L", "500g")</param>
    /// <returns>Best matched product with detailed logs</returns>
    [HttpGet("smart-match")]
    [ProducesResponseType(typeof(SmartMatchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TestSmartMatch(
        [FromQuery] string? productName,
        [FromQuery] string? quantity = null)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Product name is required",
                Message = "Please provide a product name to search"
            });
        }

        try
        {
            var smartMatching = HttpContext.RequestServices.GetRequiredService<ISmartProductMatchingService>();
            
            _logger.LogInformation("🎯 Testing Smart Product Matching for: {Product}", productName);
            
            var bestProduct = await smartMatching.FindBestMatchAsync(productName, quantity);
            
            if (bestProduct == null)
            {
                return Ok(new SmartMatchResult
                {
                    Success = false,
                    Message = "No suitable product found",
                    OriginalQuery = productName,
                    Quantity = quantity
                });
            }

            return Ok(new SmartMatchResult
            {
                Success = true,
                Message = "Best match found successfully",
                OriginalQuery = productName,
                Quantity = quantity,
                MatchedProduct = new
                {
                    bestProduct.Id,
                    bestProduct.Name,
                    bestProduct.Price,
                    bestProduct.MerchantName,
                    bestProduct.ProductUrl,
                    bestProduct.ImageUrl,
                    bestProduct.IsOnSale,
                    bestProduct.DiscountPercentage
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in smart product matching");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Error = "Smart matching failed",
                Message = ex.Message
            });
        }
    }
}

/// <summary>
/// Smart Match Result
/// </summary>
public class SmartMatchResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string OriginalQuery { get; set; } = string.Empty;
    public string? Quantity { get; set; }
    public object? MatchedProduct { get; set; }
}

/// <summary>
/// Standard error response format
/// </summary>
public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
