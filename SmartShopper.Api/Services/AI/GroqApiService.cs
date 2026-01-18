using System.Text.Json;
using System.Text;
using SmartShopper.Api.Models;

namespace SmartShopper.Api.Services;

public class GroqApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GroqApiService> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _model;

    public GroqApiService(HttpClient httpClient, ILogger<GroqApiService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Groq:ApiKey"] ?? throw new ArgumentNullException("Groq API key is required");
        _baseUrl = configuration["Groq:BaseUrl"] ?? "https://api.groq.com/openai/v1";
        _model = configuration["Groq:Model"] ?? "llama-3.1-70b-versatile";
    }

    public async Task<string> GenerateContentAsync(string prompt)
    {
        try
        {
            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                temperature = 0.7,
                max_tokens = 2000
            };

            var json = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", httpContent);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Groq API error: {StatusCode} - {Response}", response.StatusCode, responseContent);
                return string.Empty;
            }

            using var document = JsonDocument.Parse(responseContent);
            var root = document.RootElement;
            
            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var message))
                {
                    if (message.TryGetProperty("content", out var content))
                    {
                        var text = content.GetString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(text))
                        {
                            return text;
                        }
                    }
                }
            }
            
            _logger.LogWarning("Groq API returned empty or invalid response. Full response: {Response}", responseContent);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Groq API call failed");
            return string.Empty;
        }
    }

    public async Task<string> GenerateRecipeCommentAsync(string recipeName, List<string> ingredients, string instructions)
    {
        try
        {
            var prompt = $@"Aşağıdaki tarif hakkında Türkçe olarak kısa ve yararlı bir yorum yaz:

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

            return await GenerateContentAsync(prompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Groq API call failed for recipe: {RecipeName}", recipeName);
            return GetFallbackComment(recipeName, ingredients);
        }
    }

    public async Task<string> GenerateShoppingAdviceAsync(List<string> products, List<PriceComparison> priceComparisons)
    {
        try
        {
            var priceInfo = string.Join("\n", priceComparisons.Select(p => 
                $"- {p.Store}: {p.Price:F2} TL ({(p.IsAvailable ? "Stokta" : "Stokta yok")})"));

            var prompt = $@"Aşağıdaki ürünler ve fiyat karşılaştırması için Türkçe alışveriş tavsiyesi ver:

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

            return await GenerateContentAsync(prompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Groq API call failed for shopping advice");
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
