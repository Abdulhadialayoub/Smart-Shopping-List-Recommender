using SmartShopper.Api.Models;

namespace SmartShopper.Api.Services;

/// <summary>
/// Interface for Cimri.com scraping service
/// </summary>
public interface ICimriScraperService
{
    /// <summary>
    /// Searches for products on Cimri.com
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="sort">Sort parameter (default: empty)</param>
    /// <returns>Search results with products and pagination info</returns>
    Task<CimriSearchResult> SearchProductsAsync(string query, int page = 1, string sort = "");

    /// <summary>
    /// Gets detailed information about a specific product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>Detailed product information</returns>
    Task<CimriProductDetail> GetProductDetailsAsync(string productId);

    /// <summary>
    /// Gets the total number of pages from the last search
    /// </summary>
    /// <returns>Total pages count</returns>
    int GetTotalPages();
}
