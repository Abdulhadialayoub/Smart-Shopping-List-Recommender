using Microsoft.AspNetCore.Mvc;
using SmartShopper.Api.Models;
using SmartShopper.Api.Services;
using SmartShopper.Api.Services.Data;

namespace SmartShopper.Api.Controllers;

/// <summary>
/// Tarif önerisi ve besin değerleri için API endpoint'leri
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class RecipeController : ControllerBase
{
    private readonly IRecipeService _recipeService;
    private readonly IDataService _dataService;
    private readonly GeminiApiService _geminiApiService;
    private readonly GroqApiService _groqApiService;
    private readonly TelegramBotService _telegramBotService;
    private readonly ISmartProductSelectorService _smartProductSelector;
    private readonly IAIProductSelectorService _aiProductSelector;
    private readonly ILogger<RecipeController> _logger;
    private readonly string _aiProvider;

    public RecipeController(
        IRecipeService recipeService, 
        IDataService firebaseService, 
        GeminiApiService geminiApiService, 
        GroqApiService groqApiService, 
        TelegramBotService telegramBotService, 
        ISmartProductSelectorService smartProductSelector,
        IAIProductSelectorService aiProductSelector,
        ILogger<RecipeController> logger, 
        IConfiguration configuration)
    {
        _recipeService = recipeService;
        _dataService = firebaseService;
        _geminiApiService = geminiApiService;
        _groqApiService = groqApiService;
        _telegramBotService = telegramBotService;
        _smartProductSelector = smartProductSelector;
        _aiProductSelector = aiProductSelector;
        _logger = logger;
        _aiProvider = configuration["AI:Provider"] ?? "Groq";
    }

    /// <summary>
    /// Kullanıcının buzdolabındaki malzemelere göre tarif önerileri getirir
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    /// <param name="servings">Kaç kişilik tarif (varsayılan: 2)</param>
    /// <returns>Önerilen tarifler listesi</returns>
    [HttpGet("suggestions/{userId}")]
    [ProducesResponseType(typeof(RecipeSuggestionsResponse), 200)]
    public async Task<ActionResult<RecipeSuggestionsResponse>> GetRecipeSuggestions(string userId, [FromQuery] int servings = 2)
    {
        var fridgeItems = await _dataService.GetFridgeItemsAsync(userId);
        
        // Süresi geçmiş malzemeleri ayır
        var now = DateTime.UtcNow;
        var expiredItems = fridgeItems.Where(item => item.ExpiryDate < now).ToList();
        var validItems = fridgeItems.Where(item => item.ExpiryDate >= now).ToList();
        
        // Süresi yaklaşan malzemeler (3 gün içinde)
        var expiringItems = validItems.Where(item => item.ExpiryDate <= now.AddDays(3)).ToList();
        
        // Sadece geçerli malzemeleri kullan
        var availableIngredients = validItems.Select(item => item.Name).ToList();
        
        _logger.LogInformation("Tarif önerisi: {Valid} geçerli, {Expired} süresi geçmiş, {Expiring} süresi yaklaşan malzeme", 
            validItems.Count, expiredItems.Count, expiringItems.Count);
        
        var recipes = await _recipeService.GetRecipeSuggestionsAsync(availableIngredients, servings);
        
        // Her tarif için mevcut ve eksik malzemeleri hesapla
        foreach (var recipe in recipes)
        {
            recipe.AvailableIngredients = recipe.Ingredients
                .Where(ingredient => availableIngredients.Any(available => 
                    available.ToLower().Contains(ingredient.ToLower()) || 
                    ingredient.ToLower().Contains(available.ToLower())))
                .ToList();
                
            recipe.MissingIngredients = recipe.Ingredients
                .Except(recipe.AvailableIngredients)
                .ToList();
                
            recipe.MatchPercentage = recipe.AvailableIngredients.Count * 100.0 / recipe.Ingredients.Count;
        }
        
        var sortedRecipes = recipes.OrderByDescending(r => r.MatchPercentage).ToList();
        
        // Response oluştur
        var response = new RecipeSuggestionsResponse
        {
            Recipes = sortedRecipes,
            ExpiredItems = expiredItems.Select(item => new ExpiredItemInfo
            {
                Name = item.Name,
                ExpiryDate = item.ExpiryDate,
                DaysExpired = (int)(now - item.ExpiryDate).TotalDays
            }).ToList(),
            ExpiringItems = expiringItems.Select(item => new ExpiringItemInfo
            {
                Name = item.Name,
                ExpiryDate = item.ExpiryDate,
                DaysUntilExpiry = (int)(item.ExpiryDate - now).TotalDays
            }).ToList(),
            HasExpiredItems = expiredItems.Any(),
            HasExpiringItems = expiringItems.Any(),
            Message = GetExpiryMessage(expiredItems.Count, expiringItems.Count)
        };
        
        return Ok(response);
    }
    
    private string GetExpiryMessage(int expiredCount, int expiringCount)
    {
        var messages = new List<string>();
        
        if (expiredCount > 0)
        {
            messages.Add($"⚠️ {expiredCount} ürünün süresi geçmiş! Bu ürünler tarif önerilerinde kullanılmadı.");
        }
        
        if (expiringCount > 0)
        {
            messages.Add($"⏰ {expiringCount} ürünün süresi 3 gün içinde dolacak. Önce bunları kullanmayı düşünün!");
        }
        
        return messages.Any() ? string.Join(" ", messages) : "";
    }

    [HttpPost("generate")]
    public async Task<ActionResult<Recipe>> GenerateRecipe([FromBody] GenerateRecipeRequest request)
    {
        var recipe = await _recipeService.GenerateRecipeAsync(request.Ingredients, request.DietaryRestrictions);
        recipe.Nutrition = await _recipeService.GetNutritionInfoAsync(request.Ingredients);
        
        // AI ile yorum oluştur
        recipe.AiComment = _aiProvider == "Groq"
            ? await _groqApiService.GenerateRecipeCommentAsync(recipe.Name, recipe.Ingredients, string.Join(". ", recipe.Instructions))
            : await _geminiApiService.GenerateRecipeCommentAsync(recipe.Name, recipe.Ingredients, string.Join(". ", recipe.Instructions));
        recipe.CommentGeneratedAt = DateTime.UtcNow;
        
        return Ok(recipe);
    }

    /// <summary>
    /// Mevcut tarif için AI yorumu oluşturur
    /// </summary>
    [HttpPost("{recipeId}/generate-comment")]
    public async Task<ActionResult<string>> GenerateRecipeComment(string recipeId, [FromBody] Recipe recipe)
    {
        var comment = _aiProvider == "Groq"
            ? await _groqApiService.GenerateRecipeCommentAsync(recipe.Name, recipe.Ingredients, string.Join(". ", recipe.Instructions))
            : await _geminiApiService.GenerateRecipeCommentAsync(recipe.Name, recipe.Ingredients, string.Join(". ", recipe.Instructions));
            
        return Ok(new { comment, generatedAt = DateTime.UtcNow });
    }

    [HttpGet("nutrition")]
    public async Task<ActionResult<NutritionInfo>> GetNutritionInfo([FromQuery] List<string> ingredients)
    {
        var nutrition = await _recipeService.GetNutritionInfoAsync(ingredients);
        return Ok(nutrition);
    }

    /// <summary>
    /// Tarif için eksik malzemelerin alışveriş listesini ve fiyat karşılaştırmasını oluşturur ve veritabanına kaydeder
    /// </summary>
    [HttpPost("shopping-list")]
    [ProducesResponseType(typeof(RecipeShoppingListResponse), 200)]
    public async Task<ActionResult<RecipeShoppingListResponse>> CreateRecipeShoppingList([FromBody] CreateRecipeShoppingListRequest request)
    {
        try
        {
            var fridgeItems = await _dataService.GetFridgeItemsAsync(request.UserId);
            var availableIngredients = fridgeItems.Select(item => item.Name.ToLower()).ToList();
            
            // Eksik malzemeleri bul
            var missingIngredients = request.MissingIngredients
                .Where(ingredient => !availableIngredients.Any(available => 
                    available.Contains(ingredient.ToLower()) || 
                    ingredient.ToLower().Contains(available)))
                .ToList();

            if (missingIngredients.Count == 0)
            {
                return Ok(new RecipeShoppingListResponse
                {
                    RecipeName = request.RecipeName,
                    MissingIngredients = new List<string>(),
                    PriceComparisons = new List<IngredientPriceComparison>(),
                    TotalCost = 0,
                    Message = "Tüm malzemeler buzdolabınızda mevcut!",
                    ShoppingListId = null
                });
            }

            // Çok fazla eksik malzeme varsa alternatif tarif öner
            var totalIngredients = request.MissingIngredients.Count + availableIngredients.Count;
            var missingPercentage = (double)missingIngredients.Count / totalIngredients * 100;
            
            List<Recipe>? alternativeRecipes = null;
            string? alternativeMessage = null;
            
            if (missingPercentage > 50) // %50'den fazla malzeme eksikse
            {
                _logger.LogInformation("⚠️ Çok fazla eksik malzeme ({Percentage:F0}%). Alternatif tarifler aranıyor...", missingPercentage);
                
                try
                {
                    // Buzdolabındaki malzemelerle yapılabilecek tarifleri al
                    var suggestions = await _recipeService.GetRecipeSuggestionsAsync(
                        fridgeItems.Select(f => f.Name).ToList(), 
                        2);
                    
                    // En az eksik malzemesi olan 3 tarifi seç
                    alternativeRecipes = suggestions
                        .OrderBy(r => r.MissingIngredients.Count)
                        .Take(3)
                        .ToList();
                    
                    if (alternativeRecipes.Any())
                    {
                        alternativeMessage = $"⚠️ Bu tarif için {missingIngredients.Count} malzeme eksik ({missingPercentage:F0}%). " +
                                           $"Daha az malzeme gerektiren {alternativeRecipes.Count} alternatif tarif öneriyoruz!";
                        
                        _logger.LogInformation("✅ {Count} alternatif tarif bulundu", alternativeRecipes.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Alternatif tarif arama başarısız");
                }
            }

            // Her malzeme için fiyat karşılaştırması yap
            var priceComparisons = new List<IngredientPriceComparison>();
            var priceComparisonService = HttpContext.RequestServices.GetRequiredService<IPriceComparisonService>();

            _logger.LogInformation("🛒 Alışveriş listesi oluşturuluyor: {Count} eksik malzeme", missingIngredients.Count);

            foreach (var ingredient in missingIngredients)
            {
                try
                {
                    // Malzeme bilgisini akıllı şekilde parse et
                    var ingredientInfo = _smartProductSelector.ParseIngredient(ingredient);
                    
                    // Boş veya anlamsız malzemeleri atla
                    if (string.IsNullOrWhiteSpace(ingredientInfo.Name) || ingredientInfo.Name.Length < 2)
                    {
                        _logger.LogWarning("⏭️ Atlanan malzeme (anlamsız): {Original}", ingredient);
                        continue;
                    }
                    
                    // NOT: Temel malzeme kontrolü kaldırıldı - tüm malzemeler için fiyat aranacak
                    
                    _logger.LogInformation("🔍 Fiyat aranıyor: {Original} -> {Name} ({Qty} {Unit}, ~{Grams}g)", 
                        ingredient, ingredientInfo.Name, ingredientInfo.Quantity, ingredientInfo.Unit, ingredientInfo.QuantityInGrams);
                    
                    // Miktar bilgisini string olarak hazırla
                    var quantityStr = $"{ingredientInfo.Quantity} {ingredientInfo.Unit}";
                    if (ingredientInfo.QuantityInGrams > 0)
                    {
                        quantityStr += $" (~{ingredientInfo.QuantityInGrams}g)";
                    }
                    
                    var prices = await priceComparisonService.ComparePricesAsync(ingredientInfo.Name, quantityStr);
                
                if (prices.Any())
                {
                    CimriProduct? selectedProduct = null;
                    string selectionReason = "";
                    
                    // PriceComparison'ı CimriProduct'a dönüştür (AI agent için)
                    var cimriProducts = prices.Select(p => new CimriProduct
                    {
                        Name = ingredientInfo.Name,
                        Price = (decimal)p.Price,
                        MerchantName = p.Store,
                        ProductUrl = p.ProductUrl,
                        ImageUrl = p.ImageUrl,
                        IsOnSale = p.IsOnSale,
                        OriginalPrice = p.OriginalPrice.HasValue ? (decimal?)p.OriginalPrice.Value : null,
                        DiscountPercentage = p.DiscountPercentage ?? 0
                    }).ToList();
                    
                    // 🤖 AI Agent ile akıllı ürün seçimi (her zaman)
                    try
                    {
                        _logger.LogInformation("🤖 AI Agent ürün seçiyor: {Ingredient}", ingredientInfo.Name);
                        selectedProduct = await _aiProductSelector.SelectBestProductAsync(
                            ingredientInfo.Name, 
                            quantityStr, 
                            cimriProducts);
                        
                        if (selectedProduct != null)
                        {
                            selectionReason = $"AI önerisi: Bu ürün kalite-fiyat dengesi açısından en uygun seçenek.";
                            _logger.LogInformation("✅ AI seçimi: {Ingredient} - {Price} TL ({Store})", 
                                ingredientInfo.Name, selectedProduct.Price, selectedProduct.MerchantName);
                        }
                    }
                    catch (Exception aiEx)
                    {
                        _logger.LogWarning(aiEx, "⚠️ AI seçimi başarısız, akıllı skorlama kullanılıyor");
                    }
                    
                    // AI başarısız olursa akıllı skorlama kullan
                    if (selectedProduct == null)
                    {
                        var bestProduct = _smartProductSelector.SelectBestProduct(ingredientInfo, prices);
                        if (bestProduct != null)
                        {
                            selectedProduct = new CimriProduct
                            {
                                Name = ingredientInfo.Name,
                                Price = (decimal)bestProduct.Price,
                                MerchantName = bestProduct.Store,
                                ProductUrl = bestProduct.ProductUrl,
                                IsOnSale = bestProduct.IsOnSale,
                                OriginalPrice = bestProduct.OriginalPrice.HasValue ? (decimal?)bestProduct.OriginalPrice.Value : null,
                                DiscountPercentage = bestProduct.DiscountPercentage ?? 0
                            };
                            selectionReason = bestProduct.IsOnSale 
                                ? $"İndirimli ürün! %{bestProduct.DiscountPercentage} tasarruf." 
                                : "Miktar ve fiyat uygunluğu açısından en iyi seçenek.";
                        }
                    }
                    
                    if (selectedProduct != null)
                    {
                        _logger.LogInformation("✅ Seçilen ürün: {Ingredient} - {Price} TL ({Store}){Sale}", 
                            ingredientInfo.Name, selectedProduct.Price, selectedProduct.MerchantName,
                            selectedProduct.IsOnSale ? " 🏷️ İNDİRİMDE!" : "");
                        
                        priceComparisons.Add(new IngredientPriceComparison
                        {
                            Ingredient = ingredient,
                            CleanName = ingredientInfo.Name,
                            Prices = prices,
                            CheapestPrice = (double)selectedProduct.Price,
                            CheapestStore = selectedProduct.MerchantName ?? "Bilinmeyen",
                            ProductUrl = selectedProduct.ProductUrl,
                            IsOnSale = selectedProduct.IsOnSale,
                            OriginalPrice = selectedProduct.OriginalPrice.HasValue ? (double?)selectedProduct.OriginalPrice.Value : null,
                            DiscountPercentage = selectedProduct.DiscountPercentage > 0 ? (int?)selectedProduct.DiscountPercentage : null,
                            SelectionReason = selectionReason
                        });
                    }
                    else
                    {
                        // Hiç ürün seçilemediyse en ucuzu al
                            var cheapest = prices.OrderBy(p => p.Price).First();
                            priceComparisons.Add(new IngredientPriceComparison
                            {
                                Ingredient = ingredient,
                                CleanName = ingredientInfo.Name,
                                Prices = prices,
                                CheapestPrice = cheapest.Price,
                                CheapestStore = cheapest.Store,
                                ProductUrl = cheapest.ProductUrl,
                                IsOnSale = cheapest.IsOnSale,
                                OriginalPrice = cheapest.OriginalPrice,
                                DiscountPercentage = cheapest.DiscountPercentage
                            });
                        }
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Fiyat bulunamadı: {Ingredient}", ingredientInfo.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Fiyat arama hatası: {Ingredient}", ingredient);
                    // Fiyat bulunamazsa devam et
                }
            }

            _logger.LogInformation("📊 Toplam {Count} malzeme için fiyat bulundu", priceComparisons.Count);

            var totalCost = priceComparisons.Sum(p => p.CheapestPrice);

            // Alışveriş listesini veritabanına kaydet
            var shoppingList = new ShoppingList
            {
                UserId = request.UserId,
                Name = $"🍽️ {request.RecipeName} - Eksik Malzemeler",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsCompleted = false,
                EstimatedTotal = totalCost,
                Items = priceComparisons.Select(pc => new ShoppingItem
                {
                    Name = pc.CleanName,
                    Quantity = 1,
                    Unit = "adet",
                    Category = "Tarif Malzemesi",
                    IsChecked = false
                }).ToList()
            };

            var savedList = await _dataService.CreateShoppingListAsync(shoppingList);

            // Telegram bildirimi (arka planda) - Detaylı
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = HttpContext.RequestServices.CreateScope();
                    var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
                    var telegramService = scope.ServiceProvider.GetRequiredService<TelegramBotService>();
                    var user = await dataService.GetUserAsync(request.UserId);
                    
                    if (user?.TelegramChatId != null && long.TryParse(user.TelegramChatId, out long chatId))
                    {
                        // Malzeme detaylarını string olarak hazırla
                        var itemsDetails = string.Join("\n", priceComparisons.Select((item, index) => 
                            $"{index + 1}. *{item.CleanName}*\n   💰 {item.CheapestPrice:F2} TL - 🏪 {item.CheapestStore}"));
                        
                        await telegramService.NotifyRecipeShoppingListCreatedAsync(
                            chatId,
                            request.RecipeName,
                            itemsDetails,
                            totalCost
                        );
                    }
                }
                catch { }
            });

            return Ok(new RecipeShoppingListResponse
            {
                RecipeName = request.RecipeName,
                MissingIngredients = missingIngredients,
                PriceComparisons = priceComparisons,
                TotalCost = totalCost,
                Message = $"{missingIngredients.Count} malzeme için alışveriş listesi oluşturuldu",
                ShoppingListId = savedList.Id,
                HasTooManyMissingIngredients = missingPercentage > 50,
                AlternativeRecipes = alternativeRecipes,
                AlternativeRecipesMessage = alternativeMessage
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Alışveriş listesi oluşturulurken hata: " + ex.Message });
        }
    }

    private string CleanIngredientName(string ingredient)
    {
        // Miktarları ve parantez içindeki detayları temizle
        var cleaned = ingredient;
        
        // Parantez içindeki kısmı çıkar
        var parenIndex = cleaned.IndexOf('(');
        if (parenIndex > 0)
        {
            cleaned = cleaned.Substring(0, parenIndex).Trim();
        }
        
        // İki nokta sonrasını çıkar (örn: "Sos için:" -> "Sos")
        var colonIndex = cleaned.IndexOf(':');
        if (colonIndex > 0)
        {
            cleaned = cleaned.Substring(0, colonIndex).Trim();
        }
        
        // Sadece "için" veya "için:" gibi anlamsız kelimeleri atla
        if (cleaned.ToLower().Trim() == "için" || cleaned.ToLower().Trim() == "sos" || 
            cleaned.ToLower().Trim() == "üzeri" || cleaned.Length < 2)
        {
            return string.Empty;
        }
        
        // Çıkarılacak kelimeler (miktar, birim, vs.)
        var removeWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "adet", "gram", "kg", "litre", "lt", "ml", "kaşığı", "bardağı", 
            "çay", "yemek", "su", "yarım", "çeyrek", "buçuk", "tutam", "dilim",
            "tane", "demet", "diş", "dal", "yaprak", "parça", "küçük", "büyük",
            "orta", "ince", "kalın", "taze", "kuru", "için", "sos", "üzeri",
            "servis", "süsleme", "isteğe", "bağlı", "göre"
        };
        
        // Sayıları ve birimleri çıkar
        var words = cleaned.Split(new[] { ' ', ',', '/' }, StringSplitOptions.RemoveEmptyEntries);
        var meaningfulWords = words.Where(w => 
        {
            var lower = w.ToLower().Trim();
            // Sayı mı kontrol et
            if (double.TryParse(w.Replace(",", "."), out _)) return false;
            // Çıkarılacak kelime mi kontrol et
            if (removeWords.Contains(lower)) return false;
            // Çok kısa kelimeler (1 karakter)
            if (lower.Length < 2) return false;
            return true;
        }).ToList();
        
        var result = string.Join(" ", meaningfulWords).Trim();
        
        // Özel durumlar için düzeltmeler
        var specialMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "sarımsak", "sarımsak" },
            { "sarmısak", "sarımsak" },
            { "zeytinyağı", "zeytinyağı" },
            { "zeytin yağı", "zeytinyağı" },
            { "tereyağ", "tereyağı" },
            { "sıvıyağ", "sıvı yağ" },
            { "sıvı yağ", "ayçiçek yağı" },
            { "biber", "biber" },
            { "kırmızı biber", "kırmızı biber" },
            { "yeşil biber", "yeşil biber" },
            { "pul biber", "pul biber" },
            { "karabiber", "karabiber" },
            { "kara biber", "karabiber" }
        };
        
        // Özel eşleşme var mı kontrol et
        foreach (var mapping in specialMappings)
        {
            if (result.ToLower().Contains(mapping.Key.ToLower()))
            {
                return mapping.Value;
            }
        }
        
        // Eğer sonuç boşsa veya çok kısaysa, null döndür (bu malzeme atlanacak)
        if (string.IsNullOrWhiteSpace(result) || result.Length < 2)
        {
            return string.Empty;
        }
        
        return result;
    }
}

public class GenerateRecipeRequest
{
    public List<string> Ingredients { get; set; } = new();
    public string? DietaryRestrictions { get; set; }
}

public class CreateRecipeShoppingListRequest
{
    public string UserId { get; set; } = string.Empty;
    public string RecipeName { get; set; } = string.Empty;
    public List<string> MissingIngredients { get; set; } = new();
    public bool IncludeBasicIngredients { get; set; } = false; // Temel baharatları dahil et
}

public class RecipeShoppingListResponse
{
    public string RecipeName { get; set; } = string.Empty;
    public List<string> MissingIngredients { get; set; } = new();
    public List<IngredientPriceComparison> PriceComparisons { get; set; } = new();
    public double TotalCost { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ShoppingListId { get; set; }
    
    // Alternatif tarif önerileri
    public bool HasTooManyMissingIngredients { get; set; }
    public List<Recipe>? AlternativeRecipes { get; set; }
    public string? AlternativeRecipesMessage { get; set; }
    
    // Temel malzeme bilgisi
    public List<string>? BasicIngredientsExcluded { get; set; }
    public string? BasicIngredientsMessage { get; set; }
}

public class IngredientPriceComparison
{
    public string Ingredient { get; set; } = string.Empty;
    public string CleanName { get; set; } = string.Empty;
    public List<PriceComparison> Prices { get; set; } = new();
    public double CheapestPrice { get; set; }
    public string CheapestStore { get; set; } = string.Empty;
    public string? ProductUrl { get; set; }
    public bool IsOnSale { get; set; }
    public double? OriginalPrice { get; set; }
    public int? DiscountPercentage { get; set; }
    public string? SelectionReason { get; set; }
}


/// <summary>
/// Tarif önerileri response modeli
/// </summary>
public class RecipeSuggestionsResponse
{
    public List<Recipe> Recipes { get; set; } = new();
    public List<ExpiredItemInfo> ExpiredItems { get; set; } = new();
    public List<ExpiringItemInfo> ExpiringItems { get; set; } = new();
    public bool HasExpiredItems { get; set; }
    public bool HasExpiringItems { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Süresi geçmiş ürün bilgisi
/// </summary>
public class ExpiredItemInfo
{
    public string Name { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int DaysExpired { get; set; }
}

/// <summary>
/// Süresi yaklaşan ürün bilgisi
/// </summary>
public class ExpiringItemInfo
{
    public string Name { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int DaysUntilExpiry { get; set; }
}
