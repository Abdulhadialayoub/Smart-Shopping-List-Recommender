using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Cache service specifically for dual-model verification responses.
/// Implements cache key generation and TTL management for validated responses.
/// </summary>
public class VerificationCacheService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<VerificationCacheService> _logger;
    private readonly TimeSpan _defaultTtl = TimeSpan.FromHours(1);

    public VerificationCacheService(
        ICacheService cacheService,
        ILogger<VerificationCacheService> logger)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a cache key from user inventory and recipe type.
    /// Uses SHA256 hash to create a consistent, collision-resistant key.
    /// </summary>
    /// <param name="userInventory">List of ingredients in user's inventory</param>
    /// <param name="recipeType">Optional recipe type preference</param>
    /// <returns>Cache key string</returns>
    public string GenerateRecipeCacheKey(List<string> userInventory, string? recipeType = null)
    {
        // Sort inventory to ensure consistent keys regardless of input order
        var sortedInventory = userInventory
            .Select(i => i.Trim().ToLowerInvariant())
            .OrderBy(i => i)
            .ToList();

        var normalizedRecipeType = string.IsNullOrWhiteSpace(recipeType) 
            ? "any" 
            : recipeType.Trim().ToLowerInvariant();

        // Create a deterministic string representation
        var keyData = $"recipe:{normalizedRecipeType}:{string.Join(",", sortedInventory)}";

        // Hash the key to keep it manageable and consistent
        return $"verified_recipe_{ComputeHash(keyData)}";
    }

    /// <summary>
    /// Generates a cache key from shopping list.
    /// Uses SHA256 hash to create a consistent, collision-resistant key.
    /// </summary>
    /// <param name="shoppingList">List of items to purchase</param>
    /// <returns>Cache key string</returns>
    public string GenerateProductCacheKey(List<string> shoppingList)
    {
        // Sort shopping list to ensure consistent keys regardless of input order
        var sortedList = shoppingList
            .Select(i => i.Trim().ToLowerInvariant())
            .OrderBy(i => i)
            .ToList();

        // Create a deterministic string representation
        var keyData = $"products:{string.Join(",", sortedList)}";

        // Hash the key to keep it manageable and consistent
        return $"verified_products_{ComputeHash(keyData)}";
    }

    /// <summary>
    /// Retrieves a cached recipe response.
    /// </summary>
    /// <param name="userInventory">User inventory used to generate cache key</param>
    /// <param name="recipeType">Recipe type used to generate cache key</param>
    /// <returns>Cached recipe response or null if not found/expired</returns>
    public async Task<VerifiedRecipeResponse?> GetCachedRecipeAsync(
        List<string> userInventory, 
        string? recipeType = null)
    {
        try
        {
            var cacheKey = GenerateRecipeCacheKey(userInventory, recipeType);
            var cached = await _cacheService.GetAsync<VerifiedRecipeResponse>(cacheKey);

            if (cached != null)
            {
                _logger.LogInformation("Cache hit for recipe key: {Key}", cacheKey);
            }
            else
            {
                _logger.LogDebug("Cache miss for recipe key: {Key}", cacheKey);
            }

            return cached;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve cached recipe");
            return null;
        }
    }

    /// <summary>
    /// Stores a recipe response in cache with 1-hour TTL.
    /// </summary>
    /// <param name="userInventory">User inventory used to generate cache key</param>
    /// <param name="recipeType">Recipe type used to generate cache key</param>
    /// <param name="response">Recipe response to cache</param>
    public async Task SetCachedRecipeAsync(
        List<string> userInventory,
        string? recipeType,
        VerifiedRecipeResponse response)
    {
        try
        {
            var cacheKey = GenerateRecipeCacheKey(userInventory, recipeType);
            await _cacheService.SetAsync(cacheKey, response, _defaultTtl);
            
            _logger.LogInformation("Cached recipe with key: {Key}, TTL: {Ttl}", cacheKey, _defaultTtl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache recipe response");
        }
    }

    /// <summary>
    /// Retrieves a cached product recommendations response.
    /// </summary>
    /// <param name="shoppingList">Shopping list used to generate cache key</param>
    /// <returns>Cached product response or null if not found/expired</returns>
    public async Task<VerifiedProductResponse?> GetCachedProductsAsync(List<string> shoppingList)
    {
        try
        {
            var cacheKey = GenerateProductCacheKey(shoppingList);
            var cached = await _cacheService.GetAsync<VerifiedProductResponse>(cacheKey);

            if (cached != null)
            {
                _logger.LogInformation("Cache hit for products key: {Key}", cacheKey);
            }
            else
            {
                _logger.LogDebug("Cache miss for products key: {Key}", cacheKey);
            }

            return cached;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve cached products");
            return null;
        }
    }

    /// <summary>
    /// Stores a product recommendations response in cache with 1-hour TTL.
    /// </summary>
    /// <param name="shoppingList">Shopping list used to generate cache key</param>
    /// <param name="response">Product response to cache</param>
    public async Task SetCachedProductsAsync(
        List<string> shoppingList,
        VerifiedProductResponse response)
    {
        try
        {
            var cacheKey = GenerateProductCacheKey(shoppingList);
            await _cacheService.SetAsync(cacheKey, response, _defaultTtl);
            
            _logger.LogInformation("Cached products with key: {Key}, TTL: {Ttl}", cacheKey, _defaultTtl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache product response");
        }
    }

    /// <summary>
    /// Computes SHA256 hash of input string.
    /// </summary>
    private string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
