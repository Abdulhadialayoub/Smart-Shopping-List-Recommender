using Microsoft.AspNetCore.Mvc;
using SmartShopper.Api.Models;
using SmartShopper.Api.Services;
using SmartShopper.Api.Services.Data;

namespace SmartShopper.Api.Controllers;

/// <summary>
/// Buzdolabı envanter yönetimi için API endpoint'leri
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class FridgeController : ControllerBase
{
    private readonly IDataService _dataService;
    private readonly TelegramBotService _telegramBot;
    private readonly ILogger<FridgeController> _logger;

    public FridgeController(IDataService dataService, TelegramBotService telegramBot, ILogger<FridgeController> logger)
    {
        _dataService = dataService;
        _telegramBot = telegramBot;
        _logger = logger;
    }

    /// <summary>
    /// Kullanıcının buzdolabındaki tüm öğeleri getirir
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    /// <returns>Buzdolabı öğeleri listesi</returns>
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(List<FridgeItem>), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<List<FridgeItem>>> GetFridgeItems(string userId)
    {
        var items = await _dataService.GetFridgeItemsAsync(userId);
        return Ok(items);
    }

    /// <summary>
    /// Telegram Chat ID ile buzdolabı öğelerini getirir
    /// </summary>
    [HttpGet("telegram/{chatId}")]
    [ProducesResponseType(typeof(List<FridgeItem>), 200)]
    public async Task<ActionResult<List<FridgeItem>>> GetFridgeItemsByTelegram(string chatId)
    {
        try
        {
            var user = await _dataService.GetUserByTelegramIdAsync(chatId);
            if (user == null)
            {
                return Ok(new List<FridgeItem>()); // Boş liste döndür
            }
            var items = await _dataService.GetFridgeItemsAsync(user.Id);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telegram buzdolabı getirme hatası");
            return Ok(new List<FridgeItem>());
        }
    }

    /// <summary>
    /// Telegram Chat ID ile tarihi yaklaşan ürünleri getirir
    /// </summary>
    [HttpGet("telegram/{chatId}/expiring")]
    [ProducesResponseType(typeof(List<FridgeItem>), 200)]
    public async Task<ActionResult<List<FridgeItem>>> GetExpiringItemsByTelegram(string chatId, [FromQuery] int days = 5)
    {
        try
        {
            var user = await _dataService.GetUserByTelegramIdAsync(chatId);
            if (user == null)
            {
                return Ok(new List<FridgeItem>());
            }
            var items = await _dataService.GetFridgeItemsAsync(user.Id);
            var expiringItems = items.Where(item => item.DaysUntilExpiry <= days && item.DaysUntilExpiry >= 0)
                                     .OrderBy(item => item.DaysUntilExpiry)
                                     .ToList();
            return Ok(expiringItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telegram tarihi yaklaşan ürünler hatası");
            return Ok(new List<FridgeItem>());
        }
    }

    /// <summary>
    /// Buzdolabına yeni öğe ekler
    /// </summary>
    /// <param name="item">Eklenecek buzdolabı öğesi</param>
    /// <returns>Eklenen öğe</returns>
    [HttpPost]
    [ProducesResponseType(typeof(FridgeItem), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<FridgeItem>> AddFridgeItem([FromBody] FridgeItem item)
    {
        var addedItem = await _dataService.AddFridgeItemAsync(item);
        
        // Telegram bildirimi gönder (arka planda)
        _ = Task.Run(async () =>
        {
            try
            {
                // Scoped service kullanmak için yeni scope oluştur
                using var scope = HttpContext.RequestServices.CreateScope();
                var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
                var user = await dataService.GetUserAsync(item.UserId);
                
                if (user?.TelegramChatId != null && long.TryParse(user.TelegramChatId, out long chatId))
                {
                    await _telegramBot.NotifyItemAddedAsync(chatId, item.Name, item.Quantity, item.Unit);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Telegram bildirimi gönderilemedi: {ex.Message}");
            }
        });
        
        return CreatedAtAction(nameof(GetFridgeItems), new { userId = item.UserId }, addedItem);
    }

    [HttpPut("{itemId}")]
    public async Task<ActionResult<FridgeItem>> UpdateFridgeItem(string itemId, [FromBody] FridgeItem item)
    {
        item.Id = itemId;
        var updatedItem = await _dataService.UpdateFridgeItemAsync(item);
        return Ok(updatedItem);
    }

    [HttpDelete("{itemId}")]
    public async Task<ActionResult> DeleteFridgeItem(string itemId)
    {
        var result = await _dataService.DeleteFridgeItemAsync(itemId);
        if (result)
            return NoContent();
        return NotFound();
    }

    /// <summary>
    /// Son kullanma tarihi yaklaşan öğeleri getirir
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    /// <param name="days">Kaç gün içinde sona erecek öğeler (varsayılan: 3)</param>
    /// <returns>Son kullanma tarihi yaklaşan öğeler</returns>
    [HttpGet("{userId}/expiring")]
    [ProducesResponseType(typeof(List<FridgeItem>), 200)]
    public async Task<ActionResult<List<FridgeItem>>> GetExpiringItems(string userId, [FromQuery] int days = 3)
    {
        var items = await _dataService.GetFridgeItemsAsync(userId);
        var expiringItems = items.Where(item => item.DaysUntilExpiry <= days && item.DaysUntilExpiry >= 0).ToList();
        return Ok(expiringItems);
    }
    
    /// <summary>
    /// Ürün için beslenme bilgisi al (Nutrition API)
    /// Her seferinde API'den çeker, kaydetmez
    /// </summary>
    /// <param name="productName">Ürün adı</param>
    /// <returns>Beslenme bilgileri</returns>
    [HttpGet("nutrition/{productName}")]
    [ProducesResponseType(typeof(NutritionInfo), 200)]
    public async Task<ActionResult<NutritionInfo>> GetNutritionInfo(string productName, [FromServices] INutritionApiService nutritionService)
    {
        var nutrition = await nutritionService.GetNutritionInfoAsync(productName);
        
        if (nutrition == null)
        {
            return NotFound(new { error = "Beslenme bilgisi bulunamadı" });
        }
        
        return Ok(nutrition);
    }
}