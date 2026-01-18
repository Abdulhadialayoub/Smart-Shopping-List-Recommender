using Microsoft.AspNetCore.Mvc;
using SmartShopper.Api.Models;
using SmartShopper.Api.Services;
using SmartShopper.Api.Services.Data;

namespace SmartShopper.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShoppingController : ControllerBase
{
    private readonly IDataService _dataService;
    private readonly IPriceComparisonService _priceComparisonService;
    private readonly GeminiApiService _geminiApiService;
    private readonly GroqApiService _groqApiService;
    private readonly TelegramBotService _telegramBotService;
    private readonly string _aiProvider;

    public ShoppingController(
        IDataService dataService,
        IPriceComparisonService priceComparisonService,
        GeminiApiService geminiApiService,
        GroqApiService groqApiService,
        TelegramBotService telegramBotService,
        IConfiguration configuration)
    {
        _dataService = dataService;
        _priceComparisonService = priceComparisonService;
        _geminiApiService = geminiApiService;
        _groqApiService = groqApiService;
        _telegramBotService = telegramBotService;
        _aiProvider = configuration["AI:Provider"] ?? "Groq";
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<List<ShoppingList>>> GetShoppingLists(string userId)
    {
        var lists = await _dataService.GetShoppingListsAsync(userId);
        return Ok(lists);
    }

    /// <summary>
    /// Telegram Chat ID ile alışveriş listelerini getirir
    /// </summary>
    [HttpGet("telegram/{chatId}")]
    public async Task<ActionResult<List<ShoppingList>>> GetShoppingListsByTelegram(string chatId)
    {
        try
        {
            var user = await _dataService.GetUserByTelegramIdAsync(chatId);
            if (user == null)
            {
                return Ok(new List<ShoppingList>());
            }
            var lists = await _dataService.GetShoppingListsAsync(user.Id);
            return Ok(lists);
        }
        catch
        {
            return Ok(new List<ShoppingList>());
        }
    }

    [HttpPost]
    public async Task<ActionResult<ShoppingList>> CreateShoppingList([FromBody] ShoppingList shoppingList)
    {
        var createdList = await _dataService.CreateShoppingListAsync(shoppingList);
        
        // Telegram bildirimi (arka planda, scope ile)
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = HttpContext.RequestServices.CreateScope();
                var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
                var user = await dataService.GetUserAsync(shoppingList.UserId);
                
                if (user?.TelegramChatId != null && long.TryParse(user.TelegramChatId, out long chatId))
                {
                    await _telegramBotService.NotifyShoppingListCreatedAsync(
                        chatId,
                        shoppingList.Name,
                        shoppingList.Items.Count
                    );
                }
            }
            catch { }
        });
        
        return CreatedAtAction(nameof(GetShoppingLists), new { userId = shoppingList.UserId }, createdList);
    }

    [HttpPut("{listId}")]
    public async Task<ActionResult<ShoppingList>> UpdateShoppingList(string listId, [FromBody] ShoppingList shoppingList)
    {
        shoppingList.Id = listId;
        var updatedList = await _dataService.UpdateShoppingListAsync(shoppingList);
        return Ok(updatedList);
    }

    [HttpDelete("{listId}")]
    public async Task<ActionResult> DeleteShoppingList(string listId)
    {
        var result = await _dataService.DeleteShoppingListAsync(listId);
        if (result)
            return NoContent();
        return NotFound();
    }

    [HttpPost("smart-list")]
    public async Task<ActionResult<ShoppingList>> CreateSmartShoppingList([FromBody] CreateSmartListRequest request)
    {
        try
        {
            var fridgeItems = await _dataService.GetFridgeItemsAsync(request.UserId);
            
            var expiringItems = fridgeItems
                .Where(item => item.DaysUntilExpiry <= 3 && item.DaysUntilExpiry >= 0)
                .Select(item => item.Name)
                .ToList();
                
            var lowStockItems = fridgeItems
                .Where(item => item.Quantity <= 1)
                .Select(item => item.Name)
                .ToList();
            
            var availableItems = fridgeItems.Select(item => item.Name).ToList();
            
            var aiPrompt = $@"Bir akıllı alışveriş listesi oluştur.

Buzdolabında mevcut ürünler: {string.Join(", ", availableItems)}
Yakında bitecek ürünler: {string.Join(", ", expiringItems)}
Stoku azalan ürünler: {string.Join(", ", lowStockItems)}

10-15 ürünlük akıllı bir alışveriş listesi öner. JSON formatında döndür:
{{
  ""items"": [
    {{
      ""name"": ""Ürün adı"",
      ""quantity"": 1,
      ""unit"": ""adet"",
      ""category"": ""Sebze"",
      ""reason"": ""Neden önerildiği"",
      ""priority"": ""Yüksek""
    }}
  ],
  ""advice"": ""Genel alışveriş tavsiyesi""
}}";

            var aiResponse = _aiProvider == "Groq"
                ? await _groqApiService.GenerateContentAsync(aiPrompt)
                : await _geminiApiService.GenerateContentAsync(aiPrompt);
            
            var jsonStart = aiResponse.IndexOf("{");
            var jsonEnd = aiResponse.LastIndexOf("}") + 1;
            
            List<ShoppingItem> smartItems;
            string aiAdvice = "Akıllı alışveriş listesi oluşturuldu.";
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = aiResponse.Substring(jsonStart, jsonEnd - jsonStart);
                var aiResult = Newtonsoft.Json.JsonConvert.DeserializeObject<SmartListAiResponse>(jsonStr);
                
                if (aiResult?.Items != null && aiResult.Items.Count > 0)
                {
                    smartItems = aiResult.Items.Select(item => new ShoppingItem
                    {
                        Name = item.Name,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        Category = item.Category,
                        IsChecked = false
                    }).ToList();
                    
                    aiAdvice = aiResult.Advice ?? aiAdvice;
                }
                else
                {
                    smartItems = GetFallbackSmartItems(fridgeItems);
                }
            }
            else
            {
                smartItems = GetFallbackSmartItems(fridgeItems);
            }
            
            var smartList = new ShoppingList
            {
                UserId = request.UserId,
                Name = $"🤖 Akıllı Liste - {DateTime.Now:dd.MM.yyyy}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsCompleted = false,
                Items = smartItems
            };
            
            var savedList = await _dataService.CreateShoppingListAsync(smartList);
            
            // Telegram bildirimi (arka planda, scope ile)
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = HttpContext.RequestServices.CreateScope();
                    var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
                    var user = await dataService.GetUserAsync(request.UserId);
                    
                    if (user?.TelegramChatId != null && long.TryParse(user.TelegramChatId, out long chatId))
                    {
                        await _telegramBotService.NotifySmartListCreatedAsync(
                            chatId,
                            smartItems.Count,
                            aiAdvice
                        );
                    }
                }
                catch { }
            });
            
            return Ok(new 
            { 
                list = savedList,
                aiAdvice = aiAdvice,
                stats = new
                {
                    totalItems = smartItems.Count,
                    expiringItemsReplaced = expiringItems.Count,
                    lowStockItemsReplaced = lowStockItems.Count
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Akıllı liste oluşturulurken hata: " + ex.Message });
        }
    }
    
    private List<ShoppingItem> GetFallbackSmartItems(List<FridgeItem> fridgeItems)
    {
        var basicNeeds = new[] 
        { 
            ("Ekmek", "adet", "Tahıl"),
            ("Süt", "litre", "Süt Ürünleri"),
            ("Yumurta", "adet", "Diğer"),
            ("Domates", "kg", "Sebze"),
            ("Soğan", "kg", "Sebze"),
            ("Patates", "kg", "Sebze"),
            ("Tavuk", "kg", "Et"),
            ("Pirinç", "kg", "Tahıl"),
            ("Makarna", "paket", "Tahıl"),
            ("Zeytinyağı", "litre", "Diğer")
        };
        
        return basicNeeds
            .Where(item => !fridgeItems.Any(f => f.Name.ToLower().Contains(item.Item1.ToLower())))
            .Select(item => new ShoppingItem
            {
                Name = item.Item1,
                Quantity = 1,
                Unit = item.Item2,
                Category = item.Item3,
                IsChecked = false
            })
            .ToList();
    }
    
    private class SmartListAiResponse
    {
        [Newtonsoft.Json.JsonProperty("items")]
        public List<SmartListAiItem> Items { get; set; } = new();
        
        [Newtonsoft.Json.JsonProperty("advice")]
        public string? Advice { get; set; }
    }
    
    private class SmartListAiItem
    {
        [Newtonsoft.Json.JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        [Newtonsoft.Json.JsonProperty("quantity")]
        public int Quantity { get; set; }
        
        [Newtonsoft.Json.JsonProperty("unit")]
        public string Unit { get; set; } = "adet";
        
        [Newtonsoft.Json.JsonProperty("category")]
        public string Category { get; set; } = "Diğer";
        
        [Newtonsoft.Json.JsonProperty("reason")]
        public string? Reason { get; set; }
        
        [Newtonsoft.Json.JsonProperty("priority")]
        public string? Priority { get; set; }
    }

    [HttpGet("compare-prices")]
    public async Task<ActionResult<List<PriceComparison>>> ComparePrices([FromQuery] string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return BadRequest("Ürün adı boş olamaz");
        }

        try
        {
            var comparisons = await _priceComparisonService.ComparePricesAsync(productName);
            var advice = _aiProvider == "Groq"
                ? await _groqApiService.GenerateShoppingAdviceAsync(new List<string> { productName }, comparisons)
                : await _geminiApiService.GenerateShoppingAdviceAsync(new List<string> { productName }, comparisons);
            
            return Ok(new 
            { 
                comparisons, 
                aiAdvice = advice,
                generatedAt = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Fiyat karşılaştırması sırasında bir hata oluştu");
        }
    }
}

public class CreateSmartListRequest
{
    public string UserId { get; set; } = string.Empty;
}
