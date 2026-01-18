using System.Text.Json;
using System.Text;
using SmartShopper.Api.Models;

namespace SmartShopper.Api.Services;

public class GeminiApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiApiService> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _model;

    public GeminiApiService(HttpClient httpClient, ILogger<GeminiApiService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini API key is required");
        _baseUrl = configuration["Gemini:BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta";
        _model = configuration["Gemini:Model"] ?? "gemini-1.5-flash";
    }

    public async Task<string> GenerateContentAsync(string prompt)
    {
        try
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/models/{_model}:generateContent?key={_apiKey}", 
                httpContent);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {StatusCode} - {Response}", response.StatusCode, responseContent);
                return string.Empty;
            }

            using var document = JsonDocument.Parse(responseContent);
            var root = document.RootElement;
            
            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var contentElement))
                {
                    if (contentElement.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                    {
                        var firstPart = parts[0];
                        if (firstPart.TryGetProperty("text", out var textElement))
                        {
                            var text = textElement.GetString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(text))
                            {
                                return text;
                            }
                        }
                    }
                }
            }
            
            _logger.LogWarning("Gemini API returned empty or invalid response. Full response: {Response}", responseContent);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API call failed");
            return string.Empty;
        }
    }

    public async Task<string> GenerateRecipeCommentAsync(string recipeName, List<string> ingredients, string instructions)
    {
        try
        {
            var prompt = $@"
Aşağıdaki tarif hakkında Türkçe olarak kısa ve yararlı bir yorum yaz:

Tarif Adı: {recipeName}
Malzemeler: {string.Join(", ", ingredients)}
Talimatlar: {instructions}

Yorumda şunları içer:
- Tarifin zorluk seviyesi
- Tahmini hazırlama süresi
- Beslenme açısından faydaları
- Pratik ipuçları
- Hangi öğünler için uygun olduğu

Maksimum 200 kelime ile yanıtla.";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/models/{_model}:generateContent?key={_apiKey}", 
                httpContent);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Gemini API Response: {Response}", responseContent);
                
                // JSON response'u dynamic olarak parse et
                using var document = JsonDocument.Parse(responseContent);
                var root = document.RootElement;
                
                string? text = null;
                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var contentElement))
                    {
                        if (contentElement.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                        {
                            var firstPart = parts[0];
                            if (firstPart.TryGetProperty("text", out var textElement))
                            {
                                text = textElement.GetString();
                            }
                        }
                    }
                }
                
                if (string.IsNullOrEmpty(text))
                {
                    _logger.LogWarning("Gemini API returned empty text response");
                    return GetFallbackComment(recipeName, ingredients);
                }
                
                return text;
            }
            else
            {
                _logger.LogWarning("Gemini API error: {StatusCode}", response.StatusCode);
                return GetFallbackComment(recipeName, ingredients);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API call failed for recipe: {RecipeName}", recipeName);
            return GetFallbackComment(recipeName, ingredients);
        }
    }

    public async Task<string> GenerateShoppingAdviceAsync(List<string> products, List<PriceComparison> priceComparisons)
    {
        try
        {
            var priceInfo = string.Join("\n", priceComparisons.Select(p => 
                $"- {p.Store}: {p.Price:F2} TL ({(p.IsAvailable ? "Stokta" : "Stokta yok")})"));

            var prompt = $@"
Aşağıdaki ürünler ve fiyat karşılaştırması için Türkçe alışveriş tavsiyesi ver:

Ürünler: {string.Join(", ", products)}

Fiyat Karşılaştırması:
{priceInfo}

Tavsiyende şunları içer:
- En ekonomik seçenekler
- Kalite-fiyat dengesi
- Hangi marketten alışveriş yapılması önerisi
- Tasarruf ipuçları
- Toplu alım önerileri (varsa)

Maksimum 150 kelime ile yanıtla.";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/models/{_model}:generateContent?key={_apiKey}", 
                httpContent);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Gemini API Response: {Response}", responseContent);
                
                // JSON response'u dynamic olarak parse et
                using var document = JsonDocument.Parse(responseContent);
                var root = document.RootElement;
                
                string? text = null;
                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var contentElement))
                    {
                        if (contentElement.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                        {
                            var firstPart = parts[0];
                            if (firstPart.TryGetProperty("text", out var textElement))
                            {
                                text = textElement.GetString();
                            }
                        }
                    }
                }
                if (string.IsNullOrEmpty(text))
                {
                    _logger.LogWarning("Gemini API returned empty text response");
                    return GetFallbackShoppingAdvice(priceComparisons);
                }
                
                return text;
            }
            else
            {
                _logger.LogWarning("Gemini API error: {StatusCode}", response.StatusCode);
                return GetFallbackShoppingAdvice(priceComparisons);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API call failed for shopping advice");
            return GetFallbackShoppingAdvice(priceComparisons);
        }
    }

    private string GetFallbackComment(string recipeName, List<string> ingredients)
    {
        var difficulty = ingredients.Count <= 5 ? "Kolay" : ingredients.Count <= 8 ? "Orta" : "Zor";
        var estimatedTime = ingredients.Count * 5 + 15;

        return $@"{recipeName} tarifi {difficulty.ToLower()} seviyede bir tariftir. 
Yaklaşık {estimatedTime} dakikada hazırlanabilir. 
{ingredients.Count} farklı malzeme kullanılarak besleyici bir öğün elde edebilirsiniz. 
Taze malzemeler kullanmaya özen gösterin ve afiyet olsun!";
    }

    private string GetFallbackShoppingAdvice(List<PriceComparison> priceComparisons)
    {
        if (!priceComparisons.Any()) return "Fiyat bilgisi bulunamadı.";

        var cheapest = priceComparisons.OrderBy(p => p.Price).First();
        var mostExpensive = priceComparisons.OrderByDescending(p => p.Price).First();
        var savings = mostExpensive.Price - cheapest.Price;

        return $@"En uygun fiyat {cheapest.Store}'da {cheapest.Price:F2} TL. 
En pahalı seçenek {mostExpensive.Store}'da {mostExpensive.Price:F2} TL. 
{cheapest.Store}'dan alışveriş yaparak {savings:F2} TL tasarruf edebilirsiniz.";
    }
}

// Gemini API Response Models
public class GeminiResponse
{
    public GeminiCandidate[]? Candidates { get; set; }
}

public class GeminiCandidate
{
    public GeminiContent? Content { get; set; }
}

public class GeminiContent
{
    public GeminiPart[]? Parts { get; set; }
}

public class GeminiPart
{
    public string? Text { get; set; }
}