using SmartShopper.Api.Models;

namespace SmartShopper.Api.Services;

public interface IPriceComparisonService
{
    Task<List<PriceComparison>> ComparePricesAsync(string productName, string? quantity = null);
    Task<List<PriceComparison>> GetPricesFromCimriAsync(string productName, string? quantity = null);
    Task<List<PriceComparison>> GetPricesFromMigrosAsync(string productName);
    Task<List<PriceComparison>> GetPricesFromCarrefourAsync(string productName);
    Task<List<PriceComparison>> GetPricesFromBimAsync(string productName);
    Task<List<PriceComparison>> GetPricesFromA101Async(string productName);
}