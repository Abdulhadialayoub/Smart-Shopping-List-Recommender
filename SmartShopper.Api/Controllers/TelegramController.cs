using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace SmartShopper.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelegramController : ControllerBase
{
    private readonly string _botToken = "";
    private readonly HttpClient _httpClient;
    private readonly ILogger<TelegramController> _logger;

    public TelegramController(IHttpClientFactory httpClientFactory, ILogger<TelegramController> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    /// <summary>
    /// Telegram webhook endpoint - Bot'a gelen mesajları işler
    /// NOT: Webhook kullanmak için ngrok veya public URL gerekli
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] TelegramUpdate update)
    {
        try
        {
            if (update?.Message == null) return Ok();

            var chatId = update.Message.Chat.Id;
            var text = update.Message.Text ?? "";
            var username = update.Message.From?.Username ?? "Bilinmeyen";
            var firstName = update.Message.From?.FirstName ?? "";

            _logger.LogInformation($"📱 Telegram mesajı alındı - Chat ID: {chatId}, User: {username}, Text: {text}");

            string responseMessage = text.ToLower().Trim() switch
            {
                "/start" or "start" => 
                    $"👋 Merhaba {firstName}!\n\n" +
                    $"✅ Smart Shopper Bot'a hoş geldin!\n\n" +
                    $"📱 Senin Chat ID'n: `{chatId}`\n\n" +
                    $"Bu ID'yi profil ayarlarında kullanabilirsin.\n\n" +
                    $"Komutlar:\n" +
                    $"/start - Bot'u başlat\n" +
                    $"/chatid - Chat ID'ni göster\n" +
                    $"/help - Yardım",
                
                "/chatid" or "chatid" =>
                    $"📱 Senin Chat ID'n: `{chatId}`\n\n" +
                    $"Bu ID'yi kopyalayıp profil ayarlarına yapıştırabilirsin.",
                
                "/help" or "help" =>
                    $"🤖 Smart Shopper Bot Yardım\n\n" +
                    $"Bu bot, akıllı alışveriş asistanın!\n\n" +
                    $"Özellikler:\n" +
                    $"• Fiyat karşılaştırmaları\n" +
                    $"• Alışveriş önerileri\n" +
                    $"• Buzdolabı takibi\n" +
                    $"• Tarif önerileri\n\n" +
                    $"Web uygulamasından profil ayarlarına gidip Chat ID'ni ekle!",
                
                _ =>
                    $"👋 Merhaba {firstName}!\n\n" +
                    $"Mesajın için teşekkürler. Şu anda sadece temel komutları destekliyorum:\n\n" +
                    $"/start - Bot'u başlat\n" +
                    $"/chatid - Chat ID'ni göster\n" +
                    $"/help - Yardım\n\n" +
                    $"📱 Senin Chat ID'n: `{chatId}`"
            };

            await SendTelegramMessage(chatId, responseMessage);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Telegram webhook hatası: {ex.Message}");
            return Ok(); // Telegram'a her zaman 200 dön
        }
    }
    
    /// <summary>
    /// Chat ID öğrenmek için - Kullanıcı bot'a mesaj gönderdiğinde chat ID'sini döner
    /// </summary>
    [HttpGet("get-chat-id")]
    public async Task<IActionResult> GetChatId()
    {
        try
        {
            var url = $"https://api.telegram.org/bot{_botToken}/getUpdates?limit=1&offset=-1";
            var response = await _httpClient.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();
            
            return Ok(new { 
                success = true, 
                message = "Bot'a mesaj gönderdiysen, aşağıda chat ID'ni görebilirsin",
                data = result 
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Webhook'u ayarla
    /// </summary>
    [HttpPost("set-webhook")]
    public async Task<IActionResult> SetWebhook([FromBody] SetWebhookRequest request)
    {
        try
        {
            var url = $"https://api.telegram.org/bot{_botToken}/setWebhook";
            var payload = new { url = request.WebhookUrl };
            
            var response = await _httpClient.PostAsJsonAsync(url, payload);
            var result = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation($"Webhook ayarlandı: {request.WebhookUrl}");
            return Ok(new { success = true, result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Webhook'u kaldır
    /// </summary>
    [HttpPost("delete-webhook")]
    public async Task<IActionResult> DeleteWebhook()
    {
        try
        {
            var url = $"https://api.telegram.org/bot{_botToken}/deleteWebhook";
            var response = await _httpClient.PostAsync(url, null);
            var result = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("Webhook kaldırıldı");
            return Ok(new { success = true, result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Test mesajı gönder
    /// </summary>
    [HttpPost("send-test-message")]
    public async Task<IActionResult> SendTestMessage([FromBody] SendMessageRequest request)
    {
        try
        {
            await SendTelegramMessage(request.ChatId, request.Message);
            return Ok(new { success = true, message = "Mesaj gönderildi" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private async Task SendTelegramMessage(long chatId, string message)
    {
        var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
        var payload = new
        {
            chat_id = chatId,
            text = message,
            parse_mode = "Markdown"
        };

        var response = await _httpClient.PostAsJsonAsync(url, payload);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError($"Telegram mesaj gönderme hatası: {error}");
            throw new Exception($"Telegram API hatası: {error}");
        }
    }
}

// Model sınıfları
public class TelegramUpdate
{
    public int UpdateId { get; set; }
    public TelegramMessage? Message { get; set; }
}

public class TelegramMessage
{
    public int MessageId { get; set; }
    public TelegramUser? From { get; set; }
    public TelegramChat Chat { get; set; } = new();
    public long Date { get; set; }
    public string? Text { get; set; }
}

public class TelegramUser
{
    public long Id { get; set; }
    public bool IsBot { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }
}

public class TelegramChat
{
    public long Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }
    public string Type { get; set; } = "private";
}

public class SetWebhookRequest
{
    public string WebhookUrl { get; set; } = string.Empty;
}

public class SendMessageRequest
{
    public long ChatId { get; set; }
    public string Message { get; set; } = string.Empty;
}
