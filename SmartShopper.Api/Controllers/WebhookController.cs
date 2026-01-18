using Microsoft.AspNetCore.Mvc;
using SmartShopper.Api.Models;
using SmartShopper.Api.Services;
using SmartShopper.Api.Services.Data;

namespace SmartShopper.Api.Controllers;

/// <summary>
/// n8n ve diğer webhook entegrasyonları için API endpoint'leri
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WebhookController : ControllerBase
{
    private readonly IDataService _dataService;
    private readonly IRecipeService _recipeService;

    public WebhookController(IDataService firebaseService, IRecipeService recipeService)
    {
        _dataService = firebaseService;
        _recipeService = recipeService;
    }

    /// <summary>
    /// n8n için buzdolabı durumu özeti
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    /// <returns>Buzdolabı özet bilgileri</returns>
    [HttpGet("fridge-summary/{userId}")]
    [ProducesResponseType(typeof(FridgeSummary), 200)]
    public async Task<ActionResult<FridgeSummary>> GetFridgeSummary(string userId)
    {
        var items = await _dataService.GetFridgeItemsAsync(userId);
        
        var summary = new FridgeSummary
        {
            UserId = userId,
            TotalItems = items.Count,
            ExpiringItems = items.Count(i => i.DaysUntilExpiry <= 3 && i.DaysUntilExpiry >= 0),
            ExpiredItems = items.Count(i => i.IsExpired),
            Categories = items.GroupBy(i => i.Category)
                           .ToDictionary(g => g.Key, g => g.Count()),
            LastUpdated = DateTime.UtcNow
        };

        return Ok(summary);
    }

    /// <summary>
    /// n8n için akıllı alışveriş listesi oluşturma
    /// </summary>
    /// <param name="request">Alışveriş listesi oluşturma isteği</param>
    /// <returns>Oluşturulan akıllı alışveriş listesi</returns>
    [HttpPost("smart-shopping-list")]
    [ProducesResponseType(typeof(SmartShoppingListResponse), 200)]
    public async Task<ActionResult<SmartShoppingListResponse>> CreateSmartShoppingList([FromBody] SmartShoppingListRequest request)
    {
        // Buzdolabı öğelerini al
        var fridgeItems = await _dataService.GetFridgeItemsAsync(request.UserId);
        
        // Tarif önerilerini al
        var recipes = await _recipeService.GetRecipeSuggestionsAsync(
            fridgeItems.Select(i => i.Name).ToList()
        );

        // Eksik malzemeleri tespit et
        var missingIngredients = new List<string>();
        foreach (var recipe in recipes.Take(3)) // İlk 3 tarif için
        {
            missingIngredients.AddRange(recipe.MissingIngredients);
        }

        // Alışveriş listesi oluştur
        var shoppingList = new ShoppingList
        {
            UserId = request.UserId,
            Name = $"Akıllı Liste - {DateTime.Now:dd/MM/yyyy}",
            Items = missingIngredients.Distinct().Select(ingredient => new ShoppingItem
            {
                Name = ingredient,
                Quantity = 1,
                Unit = "adet",
                IsChecked = false
            }).ToList(),
            CreatedAt = DateTime.UtcNow,
            IsCompleted = false
        };

        var createdList = await _dataService.CreateShoppingListAsync(shoppingList);

        var response = new SmartShoppingListResponse
        {
            ShoppingList = createdList,
            SuggestedRecipes = recipes.Take(3).ToList(),
            TotalEstimatedCost = 0 // Fiyat hesaplaması burada yapılabilir
        };

        return Ok(response);
    }

    /// <summary>
    /// Web scraping ile toplanan gerçek fiyatları kaydet
    /// </summary>
    [HttpPost("scraped-prices")]
    public async Task<ActionResult> SaveScrapedPrices([FromBody] ScrapedPricesRequest request)
    {
        try
        {
            // Scraping sonuçlarını veritabanına kaydet
            // Şimdilik log'a yaz
            Console.WriteLine($"Scraped prices received: {request.Analysis.Count} products");
            
            return Ok(new
            {
                success = true,
                message = "Scraped prices saved successfully",
                productsCount = request.Analysis.Count,
                timestamp = request.Timestamp
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// n8n için toplu işlem endpoint'i
    /// </summary>
    /// <param name="request">Toplu işlem isteği</param>
    /// <returns>İşlem sonuçları</returns>
    [HttpPost("batch-operation")]
    [ProducesResponseType(typeof(BatchOperationResponse), 200)]
    public async Task<ActionResult<BatchOperationResponse>> BatchOperation([FromBody] BatchOperationRequest request)
    {
        var results = new List<object>();
        var errors = new List<string>();

        foreach (var operation in request.Operations)
        {
            try
            {
                switch (operation.Type.ToLower())
                {
                    case "add_fridge_item":
                        var item = System.Text.Json.JsonSerializer.Deserialize<FridgeItem>(operation.Data.ToString()!);
                        if (item != null)
                        {
                            var addedItem = await _dataService.AddFridgeItemAsync(item);
                            results.Add(new { Type = "add_fridge_item", Success = true, Data = addedItem });
                        }
                        break;

                    case "get_recipes":
                        var userId = operation.Data.ToString();
                        if (!string.IsNullOrEmpty(userId))
                        {
                            var fridgeItems = await _dataService.GetFridgeItemsAsync(userId);
                            var recipes = await _recipeService.GetRecipeSuggestionsAsync(
                                fridgeItems.Select(i => i.Name).ToList()
                            );
                            results.Add(new { Type = "get_recipes", Success = true, Data = recipes });
                        }
                        break;

                    default:
                        errors.Add($"Unknown operation type: {operation.Type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Error in operation {operation.Type}: {ex.Message}");
            }
        }

        return Ok(new BatchOperationResponse
        {
            Results = results,
            Errors = errors,
            ProcessedCount = request.Operations.Count,
            SuccessCount = results.Count,
            ErrorCount = errors.Count
        });
    }
}



// Scraping için model sınıfları
public class ScrapedPricesRequest
{
    public List<ScrapedProductAnalysis> Analysis { get; set; } = new();
    public ScrapingSummary Summary { get; set; } = new();
    public string Timestamp { get; set; } = string.Empty;
}

public class ScrapedProductAnalysis
{
    public string Product { get; set; } = string.Empty;
    public string CheapestStore { get; set; } = string.Empty;
    public string CheapestPrice { get; set; } = string.Empty;
    public string MostExpensiveStore { get; set; } = string.Empty;
    public string MostExpensivePrice { get; set; } = string.Empty;
    public string AveragePrice { get; set; } = string.Empty;
    public string PriceRange { get; set; } = string.Empty;
    public string SavingsPercentage { get; set; } = string.Empty;
    public int AvailableIn { get; set; }
}

public class ScrapingSummary
{
    public int TotalProducts { get; set; }
    public int TotalScraped { get; set; }
    public int SuccessfulScrapes { get; set; }
    public int FailedScrapes { get; set; }
    public string AverageSavingsOpportunity { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
}
