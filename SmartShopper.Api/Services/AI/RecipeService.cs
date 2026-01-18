using SmartShopper.Api.Models;
using Newtonsoft.Json;
using System.Text;

namespace SmartShopper.Api.Services;

public class RecipeService : IRecipeService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly GeminiApiService _geminiService;
    private readonly GroqApiService _groqService;
    private readonly string _aiProvider;

    public RecipeService(HttpClient httpClient, IConfiguration configuration, GeminiApiService geminiService, GroqApiService groqService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _geminiService = geminiService;
        _groqService = groqService;
        _aiProvider = configuration["AI:Provider"] ?? "Groq"; // Default: Groq
    }

    public async Task<List<Recipe>> GetRecipeSuggestionsAsync(List<string> availableIngredients, int servings = 2)
    {
        if (availableIngredients == null || availableIngredients.Count == 0)
        {
            return new List<Recipe>();
        }

        try
        {
            // Gemini AI ile DETAYLI tarif önerileri oluştur
            var prompt = $@"Sen profesyonel bir aşçısın. Buzdolabımda SADECE şu malzemeler var: {string.Join(", ", availableIngredients)}

{servings} KİŞİLİK tarifler öner.

ÖNEMLİ KURALLAR:
- SADECE buzdolabımdaki malzemeleri kullan
- Tuz, karabiber, zeytinyağı, su gibi temel mutfak malzemelerini kullanabilirsin (bunlar her evde var)
- Buzdolabımda OLMAYAN malzemeleri tarife EKLEME
- Eksik malzeme gerektiren tarifler önerme

Bu malzemelerle yapabileceğim 3-5 farklı, lezzetli ve pratik tarif öner. Her tarif için ÇOK DETAYLI bilgiler ver.

ÖRNEK FORMAT:
{{
  ""recipes"": [
    {{
      ""name"": ""Kremalı Mantar Soslu Tavuk"",
      ""description"": ""Yumuşacık tavuk göğsü, kremalı mantar sosu ile buluşuyor. Akşam yemeği için mükemmel bir seçenek."",
      ""ingredients"": [
        ""2 adet tavuk göğsü (yaklaşık 400g, ince dilimlenmiş)"",
        ""200g mantar (temizlenmiş, dilimlenmiş)"",
        ""1 su bardağı krema (200ml)"",
        ""2 yemek kaşığı tereyağı"",
        ""1 adet soğan (orta boy, ince doğranmış)"",
        ""2 diş sarımsak (ezilmiş)"",
        ""1 çay kaşığı tuz"",
        ""Yarım çay kaşığı karabiber""
      ],
      ""missingIngredients"": [
        ""1 çay kaşığı tuz"",
        ""Yarım çay kaşığı karabiber"",
        ""2 yemek kaşığı tereyağı""
      ],
      ""instructions"": [
        ""Tavuk göğüslerini iyice yıkayın ve kağıt havlu ile kurulayın."",
        ""Geniş bir tavada orta ateşte tereyağını eritin."",
        ""Tavuk dilimlerini tavaya dizin ve her iki tarafını da altın sarısı olana kadar kızartın (her taraf için 3-4 dakika)."",
        ""Aynı tavaya soğanları ekleyin ve pembeleşene kadar kavurun (5 dakika)."",
        ""Ezilmiş sarımsağı ekleyin ve 1 dakika daha kavurun."",
        ""Mantarları tavaya ekleyin ve suyunu salıp çekene kadar pişirin (8-10 dakika)."",
        ""Kremayı, tuzu ve karabiberi ekleyin. Karıştırarak kaynatın (2-3 dakika)."",
        ""Tavukları tekrar tavaya ekleyin ve 5 dakika daha pişirin."",
        ""Sıcak olarak servis edin.""
      ],
      ""prepTimeMinutes"": 15,
      ""cookTimeMinutes"": 25,
      ""servings"": {servings},
      ""difficulty"": ""Orta""
    }}
  ]
}}

ZORUNLU KURALLAR:
1. SADECE buzdolabımdaki malzemeleri kullan: {string.Join(", ", availableIngredients)}
2. Temel mutfak malzemeleri kullanabilirsin: tuz, karabiber, zeytinyağı, sıvı yağ, su, un, şeker
3. **missingIngredients**: Buzdolabımda OLMAYAN ama tarif için GEREKEN malzemeleri listele
   - Temel mutfak malzemelerini (tuz, karabiber, zeytinyağı) mutlaka ekle
   - Her malzeme için tam miktar belirt
   - Boş bırakma, en az 2-3 eksik malzeme ekle
4. Her malzemede MUTLAKA tam miktar belirt
5. Description 2-3 cümle olsun
6. Instructions EN AZ 8-10 adım olsun
7. Servings MUTLAKA {servings} kişilik olsun
8. Difficulty: ""Kolay"", ""Orta"" veya ""Zor""
9. Türk mutfağına uygun tarifler öner
10. Sadece JSON döndür, başka metin ekleme.";

            var aiResponse = _aiProvider == "Groq" 
                ? await _groqService.GenerateContentAsync(prompt)
                : await _geminiService.GenerateContentAsync(prompt);
            
            // JSON'u parse et - daha robust parsing
            var jsonStr = ExtractJsonFromResponse(aiResponse);
            
            if (!string.IsNullOrEmpty(jsonStr))
            {
                try
                {
                    var result = JsonConvert.DeserializeObject<RecipeResponse>(jsonStr);
                    
                    if (result?.Recipes != null && result.Recipes.Count > 0)
                    {
                        // ID'leri ekle
                        for (int i = 0; i < result.Recipes.Count; i++)
                        {
                            result.Recipes[i].Id = Guid.NewGuid().ToString();
                        }
                        
                        return result.Recipes;
                    }
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine($"JSON parse hatası: {jsonEx.Message}");
                    Console.WriteLine($"JSON içeriği: {jsonStr.Substring(0, Math.Min(500, jsonStr.Length))}...");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AI tarif önerisi hatası: {ex.Message}");
        }

        // AI başarısız olursa fallback tarifler döndür
        return GetFallbackRecipes(availableIngredients);
    }

    private List<Recipe> GetFallbackRecipes(List<string> availableIngredients)
    {
        var recipes = new List<Recipe>();
        var lowerIngredients = availableIngredients.Select(i => i.ToLower()).ToList();
        
        // Domates + Soğan kombinasyonu
        if (lowerIngredients.Any(i => i.Contains("domates")) && lowerIngredients.Any(i => i.Contains("soğan")))
        {
            recipes.Add(new Recipe
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Çoban Salatası",
                Description = "Türk mutfağının vazgeçilmez salatası. Taze sebzelerle hazırlanan, hafif ve sağlıklı bir seçenek. Her öğüne eşlik edebilir.",
                Ingredients = new List<string> 
                { 
                    "2 adet domates (orta boy, küp doğranmış)",
                    "1 adet soğan (orta boy, ince doğranmış)",
                    "1 adet salatalık (küp doğranmış)",
                    "2 yemek kaşığı zeytinyağı",
                    "1 yemek kaşığı limon suyu",
                    "1 çay kaşığı tuz",
                    "Yarım çay kaşığı karabiber",
                    "Taze maydanoz (ince kıyılmış)"
                },
                Instructions = new List<string> 
                { 
                    "Domatesleri iyice yıkayın ve kağıt havlu ile kurulayın",
                    "Domatesleri 1 cm küpler halinde doğrayın",
                    "Soğanı ince ince doğrayın ve 5 dakika suda bekletin (acılığı gitmesi için)",
                    "Salatalığı küp küp doğrayın",
                    "Tüm sebzeleri derin bir kaseye alın",
                    "Üzerine zeytinyağı, limon suyu, tuz ve karabiberi ekleyin",
                    "Tahta kaşıkla nazikçe karıştırın",
                    "Üzerine ince kıyılmış maydanoz serpin",
                    "5 dakika dinlendirin ve servis edin"
                },
                PrepTimeMinutes = 15,
                CookTimeMinutes = 0,
                Servings = 2,
                Difficulty = "Kolay"
            });
        }

        // Yumurta tarifleri
        if (lowerIngredients.Any(i => i.Contains("yumurta")))
        {
            recipes.Add(new Recipe
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Menemen",
                Description = "Türk kahvaltısının vazgeçilmezi. Domates, biber ve yumurtanın mükemmel uyumu. Sıcak ekmek ile harika gider.",
                Ingredients = new List<string> 
                { 
                    "3 adet yumurta",
                    "2 adet domates (orta boy, doğranmış)",
                    "1 adet yeşil biber (ince doğranmış)",
                    "1 yemek kaşığı tereyağı",
                    "1 çay kaşığı tuz",
                    "Yarım çay kaşığı karabiber",
                    "Yarım çay kaşığı pul biber"
                },
                Instructions = new List<string> 
                { 
                    "Domatesleri küp küp doğrayın",
                    "Yeşil biberi ince ince doğrayın",
                    "Tavada orta ateşte tereyağını eritin (1-2 dakika)",
                    "Biberleri tavaya ekleyin ve 2 dakika kavurun",
                    "Domatesleri ekleyin ve suyunu salıp çekene kadar pişirin (8-10 dakika)",
                    "Tuz, karabiber ve pul biberi ekleyin",
                    "Yumurtaları kırıp tavaya ekleyin",
                    "Tahta kaşıkla nazikçe karıştırarak pişirin (3-4 dakika)",
                    "Yumurtalar pişince ateşten alın",
                    "Sıcak olarak servis edin"
                },
                PrepTimeMinutes = 10,
                CookTimeMinutes = 15,
                Servings = 2,
                Difficulty = "Kolay"
            });
        }

        // Tavuk tarifleri
        if (lowerIngredients.Any(i => i.Contains("tavuk")))
        {
            recipes.Add(new Recipe
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Tavuk Sote",
                Description = "Pratik ve lezzetli bir akşam yemeği. Yumuşacık tavuk parçaları sebzelerle buluşuyor. Pilav veya makarna ile mükemmel.",
                Ingredients = new List<string> 
                { 
                    "400g tavuk göğsü (küp doğranmış)",
                    "1 adet soğan (orta boy, doğranmış)",
                    "1 adet domates (doğranmış)",
                    "1 adet biber (doğranmış)",
                    "2 yemek kaşığı sıvı yağ",
                    "1 çay kaşığı tuz",
                    "1 çay kaşığı karabiber",
                    "1 çay kaşığı kırmızı toz biber",
                    "Yarım su bardağı su"
                },
                Instructions = new List<string> 
                { 
                    "Tavuk göğsünü 2 cm küpler halinde doğrayın",
                    "Geniş bir tavada orta ateşte yağı ısıtın",
                    "Tavuk parçalarını tavaya ekleyin ve her tarafını kızartın (5-6 dakika)",
                    "Soğanları ekleyin ve pembeleşene kadar kavurun (3-4 dakika)",
                    "Biberleri ekleyin ve 2 dakika kavurun",
                    "Domatesleri ekleyin ve 5 dakika pişirin",
                    "Tuz ve baharatları ekleyin",
                    "Yarım su bardağı su ekleyin ve kapağı kapatın",
                    "Kısık ateşte 15 dakika pişirin",
                    "Ara sıra karıştırın ve servis edin"
                },
                PrepTimeMinutes = 15,
                CookTimeMinutes = 30,
                Servings = 3,
                Difficulty = "Orta"
            });
        }

        // Makarna tarifleri
        if (lowerIngredients.Any(i => i.Contains("makarna")))
        {
            recipes.Add(new Recipe
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Domates Soslu Makarna",
                Description = "Klasik İtalyan lezzetinin Türk dokunuşlu versiyonu. Hızlı, pratik ve çok lezzetli. Çocukların da favorisi.",
                Ingredients = new List<string> 
                { 
                    "250g makarna",
                    "3 adet domates (rendelenmiş)",
                    "2 diş sarımsak (ezilmiş)",
                    "2 yemek kaşığı zeytinyağı",
                    "1 çay kaşığı tuz",
                    "1 çay kaşığı şeker",
                    "Taze fesleğen yaprakları",
                    "Rendelenmiş kaşar peyniri"
                },
                Instructions = new List<string> 
                { 
                    "Büyük bir tencerede bol su kaynatın ve tuzlayın",
                    "Makarnayı kaynar suya atın ve paket üzerindeki süre kadar haşlayın (8-10 dakika)",
                    "Ayrı bir tavada zeytinyağını ısıtın",
                    "Ezilmiş sarımsağı tavaya ekleyin ve 1 dakika kavurun",
                    "Rendelenmiş domatesleri ekleyin",
                    "Tuz ve şekeri ekleyin, karıştırın",
                    "Orta ateşte 15 dakika pişirin (ara sıra karıştırarak)",
                    "Haşlanan makarnayı süzün ve sosu üzerine dökün",
                    "İyice karıştırın",
                    "Servis tabağına alın, üzerine fesleğen ve kaşar peyniri serpin"
                },
                PrepTimeMinutes = 10,
                CookTimeMinutes = 20,
                Servings = 2,
                Difficulty = "Kolay"
            });
        }

        // Hiçbir özel malzeme yoksa genel tarif
        if (recipes.Count == 0)
        {
            recipes.Add(new Recipe
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Karışık Sebze Salatası",
                Description = "Buzdolabınızdaki taze sebzelerle hazırlayabileceğiniz sağlıklı ve hafif bir salata. Her öğüne eşlik edebilir.",
                Ingredients = availableIngredients.Take(6).Select(i => $"1 porsiyon {i}").ToList(),
                Instructions = new List<string> 
                { 
                    "Tüm sebzeleri iyice yıkayın ve kurulayın",
                    "Sebzeleri uygun şekilde doğrayın (küp, dilim veya julyen)",
                    "Derin bir salata kasesine alın",
                    "Üzerine 2 yemek kaşığı zeytinyağı dökün",
                    "1 yemek kaşığı limon suyu ekleyin",
                    "Tuz ve karabiber ekleyin",
                    "Tahta kaşıkla nazikçe karıştırın",
                    "5 dakika dinlendirin",
                    "Servis edin ve afiyet olsun"
                },
                PrepTimeMinutes = 15,
                CookTimeMinutes = 0,
                Servings = 2,
                Difficulty = "Kolay"
            });
        }

        return recipes;
    }

    private class RecipeResponse
    {
        [JsonProperty("recipes")]
        public List<Recipe> Recipes { get; set; } = new();
    }

    /// <summary>
    /// AI yanıtından JSON'u güvenli şekilde çıkarır
    /// </summary>
    private string ExtractJsonFromResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return string.Empty;

        try
        {
            // Markdown code block içindeki JSON'u bul
            var codeBlockStart = response.IndexOf("```json");
            if (codeBlockStart >= 0)
            {
                var jsonStart = response.IndexOf("{", codeBlockStart);
                var codeBlockEnd = response.IndexOf("```", jsonStart);
                if (jsonStart >= 0 && codeBlockEnd > jsonStart)
                {
                    return response.Substring(jsonStart, codeBlockEnd - jsonStart).Trim();
                }
            }

            // Normal JSON objesi bul
            var firstBrace = response.IndexOf("{");
            if (firstBrace < 0)
                return string.Empty;

            // Balanced braces ile JSON'un sonunu bul
            int braceCount = 0;
            int jsonEnd = -1;
            
            for (int i = firstBrace; i < response.Length; i++)
            {
                if (response[i] == '{')
                    braceCount++;
                else if (response[i] == '}')
                {
                    braceCount--;
                    if (braceCount == 0)
                    {
                        jsonEnd = i + 1;
                        break;
                    }
                }
            }

            if (jsonEnd > firstBrace)
            {
                return response.Substring(firstBrace, jsonEnd - firstBrace);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"JSON extraction hatası: {ex.Message}");
        }

        return string.Empty;
    }

    public async Task<NutritionInfo> GetNutritionInfoAsync(List<string> ingredients)
    {
        // Malzemelerin ortalama besin değerlerini hesapla
        // Her malzeme için USDA API'den veri çekip ortalamasını al
        
        var totalNutrition = new NutritionInfo();
        int successCount = 0;

        // Not: Bu basitleştirilmiş bir implementasyon
        // Gerçek uygulamada her malzeme için ayrı ayrı API çağrısı yapılmalı
        
        return new NutritionInfo
        {
            Calories = 200,
            Protein = 10,
            Carbohydrates = 25,
            Fat = 8,
            Fiber = 4,
            Sugar = 3,
            Salt = 0.5
        };
    }

    public async Task<Recipe> GenerateRecipeAsync(List<string> ingredients, string? dietaryRestrictions = null)
    {
        try
        {
            var prompt = $@"Sen profesyonel bir aşçısın. Bu malzemelerle lezzetli bir tarif oluştur: {string.Join(", ", ingredients)}
{(!string.IsNullOrEmpty(dietaryRestrictions) ? $"\nDiyet kısıtlamaları: {dietaryRestrictions}" : "")}

ÇOK DETAYLI bir tarif hazırla. Şu formatta JSON döndür:
{{
  ""name"": ""Yaratıcı ve Çekici Tarif Adı"",
  ""description"": ""Tarifin detaylı açıklaması (2-3 cümle, tadı, dokusu, özel özellikleri)"",
  ""ingredients"": [
    ""2 adet domates (orta boy, küp doğranmış)"",
    ""1 yemek kaşığı zeytinyağı"",
    ""200g tavuk göğsü (ince dilimlenmiş)""
  ],
  ""missingIngredients"": [
    ""1 çay kaşığı tuz"",
    ""Yarım çay kaşığı karabiber"",
    ""2 yemek kaşığı zeytinyağı""
  ],
  ""instructions"": [
    ""Malzemeleri hazırlayın: Domatesleri yıkayın ve 1 cm küpler halinde doğrayın"",
    ""Tavada orta ateşte zeytinyağını ısıtın (1-2 dakika)"",
    ""Tavuk dilimlerini tavaya ekleyin ve her iki tarafını 4-5 dakika kızartın"",
    ""Domatesleri ekleyin ve 10 dakika pişirin (ara sıra karıştırarak)"",
    ""Tuz ve baharatları ekleyin, 2 dakika daha pişirin"",
    ""Ateşten alın, 3 dakika dinlendirin"",
    ""Sıcak olarak servis edin, yanında pilav veya ekmek ile""
  ],
  ""prepTimeMinutes"": 15,
  ""cookTimeMinutes"": 25,
  ""servings"": 2,
  ""difficulty"": ""Orta""
}}

KURALLAR:
1. Malzemelerde tam miktar ve detay ver (parantez içinde)
2. En az 7-10 adım yaz, her adım çok detaylı olsun
3. Süre ve ısı bilgisi ver (5 dakika, orta ateşte vb.)
4. Türk mutfağına uygun olsun
5. **missingIngredients**: Kullanıcının VERMEDİĞİ ama tarif için GEREKEN malzemeleri listele
   - Temel mutfak malzemeleri (tuz, karabiber, zeytinyağı, su) EKLE
   - Her malzeme için tam miktar belirt
   - Boş array bırakma, mutlaka eksik malzeme ekle
6. Sadece JSON döndür.";

            var aiResponse = _aiProvider == "Groq" 
                ? await _groqService.GenerateContentAsync(prompt)
                : await _geminiService.GenerateContentAsync(prompt);
            
            var jsonStr = ExtractJsonFromResponse(aiResponse);
            
            if (!string.IsNullOrEmpty(jsonStr))
            {
                try
                {
                    var recipe = JsonConvert.DeserializeObject<Recipe>(jsonStr);
                    
                    if (recipe != null)
                    {
                        recipe.Id = Guid.NewGuid().ToString();
                        return recipe;
                    }
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine($"JSON parse hatası (GenerateRecipe): {jsonEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AI tarif oluşturma hatası: {ex.Message}");
        }

        // Fallback
        return new Recipe
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Özel Tarif",
            Description = "Seçtiğiniz malzemelerle hazırlanabilecek tarif",
            Ingredients = ingredients,
            Instructions = new List<string> 
            { 
                "Malzemeleri hazırlayın",
                "Uygun şekilde pişirin",
                "Servis edin ve afiyet olsun"
            },
            PrepTimeMinutes = 15,
            CookTimeMinutes = 30,
            Servings = 2,
            Difficulty = "Orta"
        };
    }
}