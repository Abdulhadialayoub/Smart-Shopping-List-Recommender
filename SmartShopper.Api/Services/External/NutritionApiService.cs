using System.Text.Json;

namespace SmartShopper.Api.Services;

/// <summary>
/// Ürün beslenme bilgileri (100g başına)
/// </summary>
public class NutritionInfo
{
    public double Calories { get; set; }  // Kalori (kcal)
    public double Protein { get; set; }  // Protein (g)
    public double Carbohydrates { get; set; }  // Karbonhidrat (g)
    public double Fat { get; set; }  // Yağ (g)
    public double Fiber { get; set; }  // Lif (g)
    public double Sugar { get; set; }  // Şeker (g)
    public double Salt { get; set; }  // Tuz (g)
}

public interface INutritionApiService
{
    Task<NutritionInfo?> GetNutritionInfoAsync(string productName);
}

public class NutritionApiService : INutritionApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NutritionApiService> _logger;
    private readonly string _apiKey;

    public NutritionApiService(HttpClient httpClient, IConfiguration configuration, ILogger<NutritionApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["NutritionAPI:ApiKey"] ?? "";
        
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogInformation("API Ninjas Nutrition API yapılandırıldı");
        }
    }

    public async Task<NutritionInfo?> GetNutritionInfoAsync(string productName)
    {
        try
        {
            // USDA FoodData Central API kullanıyoruz (ücretsiz ve sınırsız)
            // https://fdc.nal.usda.gov/api-guide.html
            
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogError("Nutrition API key bulunamadı!");
                return null;
            }

            // 1. Önce ürünü ara
            var searchUrl = $"https://api.nal.usda.gov/fdc/v1/foods/search?api_key={_apiKey}&query={Uri.EscapeDataString(productName)}&pageSize=1";
            var searchResponse = await _httpClient.GetAsync(searchUrl);
            
            if (!searchResponse.IsSuccessStatusCode)
            {
                _logger.LogError($"Nutrition API arama hatası: {searchResponse.StatusCode}");
                return null;
            }

            var searchJson = await searchResponse.Content.ReadAsStringAsync();
            _logger.LogInformation($"Search Response: {searchJson.Substring(0, Math.Min(500, searchJson.Length))}...");
            
            using var searchDoc = JsonDocument.Parse(searchJson);
            var foods = searchDoc.RootElement.GetProperty("foods");
            
            if (foods.GetArrayLength() == 0)
            {
                _logger.LogWarning($"'{productName}' için beslenme bilgisi bulunamadı");
                return null;
            }

            var food = foods[0];
            var nutrients = food.GetProperty("foodNutrients");
            
            // Besin değerlerini topla
            var nutritionData = new Dictionary<string, double>();
            
            foreach (var nutrient in nutrients.EnumerateArray())
            {
                var nutrientName = GetStringValue(nutrient, "nutrientName");
                var value = GetDoubleValue(nutrient, "value");
                
                // Nutrient ID'lerine göre eşleştir
                if (nutrient.TryGetProperty("nutrientId", out var idProp))
                {
                    var id = idProp.GetInt32();
                    switch (id)
                    {
                        case 1008: // Energy (kcal)
                            nutritionData["calories"] = value;
                            break;
                        case 1003: // Protein
                            nutritionData["protein"] = value;
                            break;
                        case 1005: // Carbohydrates
                            nutritionData["carbs"] = value;
                            break;
                        case 1004: // Total Fat
                            nutritionData["fat"] = value;
                            break;
                        case 1079: // Fiber
                            nutritionData["fiber"] = value;
                            break;
                        case 2000: // Sugars
                            nutritionData["sugar"] = value;
                            break;
                        case 1093: // Sodium
                            nutritionData["sodium"] = value / 1000.0; // mg to g
                            break;
                    }
                }
            }
            
            return new NutritionInfo
            {
                Calories = nutritionData.GetValueOrDefault("calories", 0),
                Protein = nutritionData.GetValueOrDefault("protein", 0),
                Carbohydrates = nutritionData.GetValueOrDefault("carbs", 0),
                Fat = nutritionData.GetValueOrDefault("fat", 0),
                Fiber = nutritionData.GetValueOrDefault("fiber", 0),
                Sugar = nutritionData.GetValueOrDefault("sugar", 0),
                Salt = nutritionData.GetValueOrDefault("sodium", 0)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Nutrition API hatası: {ex.Message}");
            return null;
        }
    }
    
    private static string GetStringValue(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return string.Empty;
            
        return property.ValueKind == JsonValueKind.String ? property.GetString() ?? string.Empty : string.Empty;
    }

    private static double GetDoubleValue(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return 0;

        return property.ValueKind switch
        {
            JsonValueKind.Number => property.GetDouble(),
            JsonValueKind.String => double.TryParse(property.GetString(), 
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, 
                out var result) ? result : 0,
            _ => 0
        };
    }
}
