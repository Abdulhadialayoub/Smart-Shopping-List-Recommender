using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SmartShopper.Api.Services.Data;

namespace SmartShopper.Api.Services;

/// <summary>
/// Telegram bildirim servisi.
/// n8n entegrasyonu sonrası artık polling yapmıyor, sadece bildirim göndermek için kullanılıyor.
/// </summary>
public class TelegramBotService
{
    private readonly string _botToken;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TelegramBotService> _logger;
    private readonly Dictionary<string, DateTime> _lastNotificationTime = new();
    private readonly TimeSpan _notificationCooldown = TimeSpan.FromMinutes(2);

    public TelegramBotService(IHttpClientFactory httpClientFactory, ILogger<TelegramBotService> logger, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
        _botToken = configuration["Telegram:BotToken"] ?? "";
    }

    // Polling logic removed as it is now handled by n8n via Webhook.

    private async Task SendAsync(long chatId, string text)
    {
        try
        {
            if (string.IsNullOrEmpty(_botToken)) return;
            
            var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
            await _httpClient.PostAsJsonAsync(url, new { chat_id = chatId, text, parse_mode = "Markdown" });
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Mesaj gönderilemedi"); }
    }

    // Public notification methods
    public Task SendNotificationAsync(long chatId, string msg) => SendAsync(chatId, msg);
    
    public async Task NotifyItemAddedAsync(long chatId, string name, int qty, string unit)
    {
        if (!CanNotify($"item_{chatId}_{name}")) return;
        await SendAsync(chatId, $"✅ *Eklendi*\n\n📦 {name} ({qty} {unit})");
        SetNotified($"item_{chatId}_{name}");
    }
    
    public async Task NotifyItemExpiringSoonAsync(long chatId, string name, int days)
    {
        if (!CanNotify($"exp_{chatId}_{name}")) return;
        await SendAsync(chatId, $"⚠️ *Uyarı*\n\n📦 {name}\n⏰ {days} gün kaldı!");
        SetNotified($"exp_{chatId}_{name}");
    }
    
    public async Task NotifyShoppingListCreatedAsync(long chatId, string name, int count)
    {
        if (!CanNotify($"list_{chatId}")) return;
        await SendAsync(chatId, $"🛒 *Liste*\n\n📋 {name}\n📦 {count} ürün");
        SetNotified($"list_{chatId}");
    }
    
    public async Task NotifyRecipeShoppingListCreatedAsync(long chatId, string recipe, string items, double total)
    {
        if (!CanNotify($"recipe_{chatId}")) return;
        await SendAsync(chatId, $"🍽️ *Tarif Listesi*\n\n📋 {recipe}\n\n{items}\n\n💰 {total:F2} TL");
        SetNotified($"recipe_{chatId}");
    }
    
    public async Task NotifySmartListCreatedAsync(long chatId, int count, string advice)
    {
        if (!CanNotify($"smart_{chatId}")) return;
        await SendAsync(chatId, $"🤖 *Akıllı Liste*\n\n📦 {count} ürün\n💡 {advice}");
        SetNotified($"smart_{chatId}");
    }
    
    public async Task NotifyRecipeSuggestionAsync(long chatId, string name, int match)
    {
        if (!CanNotify($"sug_{chatId}")) return;
        await SendAsync(chatId, $"👨‍🍳 *Tarif*\n\n🍽️ {name}\n✅ %{match}");
        SetNotified($"sug_{chatId}");
    }
    
    public async Task NotifyPriceAlertAsync(long chatId, string product, double old, double now, string store)
    {
        if (!CanNotify($"price_{chatId}_{product}")) return;
        await SendAsync(chatId, $"💰 *İndirim!*\n\n📦 {product}\n🏪 {store}\n💵 {old:F2} → {now:F2} TL");
        SetNotified($"price_{chatId}_{product}");
    }
    
    private bool CanNotify(string key)
    {
        lock (_lastNotificationTime)
        {
            return !_lastNotificationTime.TryGetValue(key, out var t) || DateTime.UtcNow - t > _notificationCooldown;
        }
    }
    
    private void SetNotified(string key)
    {
        lock (_lastNotificationTime)
        {
            _lastNotificationTime[key] = DateTime.UtcNow;
            var old = _lastNotificationTime.Where(x => DateTime.UtcNow - x.Value > TimeSpan.FromHours(1)).Select(x => x.Key).ToList();
            foreach (var k in old) _lastNotificationTime.Remove(k);
        }
    }
}
// Removed unused classes related to polling (TelegramUpdatesResponse, etc)
