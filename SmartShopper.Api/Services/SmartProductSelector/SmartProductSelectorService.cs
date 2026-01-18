using SmartShopper.Api.Models;
using System.Text.RegularExpressions;

namespace SmartShopper.Api.Services;

/// <summary>
/// Tarif malzemelerine göre en uygun ürünü seçen akıllı servis
/// </summary>
public interface ISmartProductSelectorService
{
    /// <summary>
    /// Malzeme string'inden miktar ve birim bilgisini çıkarır
    /// </summary>
    IngredientInfo ParseIngredient(string ingredient);
    
    /// <summary>
    /// Verilen ürünler arasından malzeme için en uygun olanı seçer
    /// </summary>
    PriceComparison? SelectBestProduct(IngredientInfo ingredient, List<PriceComparison> products);
    
    /// <summary>
    /// Ürün adından gramaj/miktar bilgisini çıkarır
    /// </summary>
    ProductSizeInfo ParseProductSize(string productName);
    
    /// <summary>
    /// Malzemenin evde genellikle bulunan temel malzeme olup olmadığını kontrol eder
    /// </summary>
    bool IsBasicHomeIngredient(string ingredientName);
}

public class SmartProductSelectorService : ISmartProductSelectorService
{
    private readonly ILogger<SmartProductSelectorService> _logger;
    
    // Evde genellikle bulunan temel malzemeler (SADECE çok temel olanlar)
    // NOT: Un, şeker, yağ gibi malzemeler çıkarıldı - herkesin evinde olmayabilir
    private static readonly HashSet<string> BasicHomeIngredients = new(StringComparer.OrdinalIgnoreCase)
    {
        // Su - herkesin evinde var
        "su", "içme suyu", "sıcak su", "soğuk su", "kaynar su",
        
        // Sadece çok temel baharatlar
        "tuz", "sofra tuzu", "iyotlu tuz",
        "karabiber", "kara biber", "toz karabiber",
        
        // Sirke (genellikle evde bulunur)
        "sirke", "elma sirkesi"
    };

    public SmartProductSelectorService(ILogger<SmartProductSelectorService> logger)
    {
        _logger = logger;
    }
    
    public bool IsBasicHomeIngredient(string ingredientName)
    {
        if (string.IsNullOrWhiteSpace(ingredientName)) return false;
        
        var lower = ingredientName.ToLower().Trim();
        
        // Direkt eşleşme
        if (BasicHomeIngredients.Contains(lower))
        {
            _logger.LogInformation("🏠 Temel malzeme (evde var): {Name}", ingredientName);
            return true;
        }
        
        // Kısmi eşleşme (örn: "1 tutam tuz" -> "tuz" içeriyor)
        foreach (var basic in BasicHomeIngredients)
        {
            if (lower.Contains(basic) || basic.Contains(lower))
            {
                // Ama "zeytinyağı" gibi özel durumları kontrol et
                if (basic == "yağ" && (lower.Contains("tereyağ") || lower.Contains("zeytin")))
                {
                    continue; // Bu temel malzeme değil
                }
                
                _logger.LogInformation("🏠 Temel malzeme (evde var): {Name} (eşleşme: {Basic})", ingredientName, basic);
                return true;
            }
        }
        
        return false;
    }

    public IngredientInfo ParseIngredient(string ingredient)
    {
        var info = new IngredientInfo { OriginalText = ingredient };
        
        // Parantez içini çıkar
        var cleaned = Regex.Replace(ingredient, @"\([^)]*\)", "").Trim();
        
        // Miktar pattern'leri
        var patterns = new[]
        {
            // "200 gram makarna", "200g makarna"
            @"(\d+(?:[.,]\d+)?)\s*(gram|gr|g)\s+(.+)",
            // "1 kg un", "1.5kg un"
            @"(\d+(?:[.,]\d+)?)\s*(kg|kilo)\s+(.+)",
            // "500 ml süt", "1 litre süt"
            @"(\d+(?:[.,]\d+)?)\s*(ml|litre|lt|l)\s+(.+)",
            // "2 adet yumurta"
            @"(\d+(?:[.,]\d+)?)\s*(adet|tane)\s+(.+)",
            // "1 su bardağı pirinç"
            @"(\d+(?:[.,]\d+)?)\s*(su\s+bardağı|çay\s+bardağı|bardak)\s+(.+)",
            // "2 yemek kaşığı yağ"
            @"(\d+(?:[.,]\d+)?)\s*(yemek\s+kaşığı|çay\s+kaşığı|kaşık|tatlı\s+kaşığı)\s+(.+)",
            // "1 tutam tuz"
            @"(\d+(?:[.,]\d+)?)\s*(tutam|çimdik)\s+(.+)",
            // "3 diş sarımsak"
            @"(\d+(?:[.,]\d+)?)\s*(diş)\s+(.+)",
            // "1 demet maydanoz"
            @"(\d+(?:[.,]\d+)?)\s*(demet|dal|yaprak)\s+(.+)",
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(cleaned, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                info.Quantity = double.Parse(match.Groups[1].Value.Replace(",", "."));
                info.Unit = NormalizeUnit(match.Groups[2].Value);
                info.Name = match.Groups[3].Value.Trim();
                info.QuantityInGrams = ConvertToGrams(info.Quantity, info.Unit, info.Name);
                return info;
            }
        }

        // Pattern eşleşmezse, sadece sayıyı ve geri kalanı al
        var simpleMatch = Regex.Match(cleaned, @"^(\d+(?:[.,]\d+)?)\s+(.+)$");
        if (simpleMatch.Success)
        {
            info.Quantity = double.Parse(simpleMatch.Groups[1].Value.Replace(",", "."));
            info.Unit = "adet";
            info.Name = simpleMatch.Groups[2].Value.Trim();
        }
        else
        {
            // Hiç miktar yoksa
            info.Name = CleanIngredientName(cleaned);
            info.Quantity = 1;
            info.Unit = "adet";
        }

        return info;
    }

    public PriceComparison? SelectBestProduct(IngredientInfo ingredient, List<PriceComparison> products)
    {
        if (!products.Any()) return null;

        _logger.LogInformation("🎯 Ürün seçimi: {Name}, İhtiyaç: {Qty} {Unit} (~{Grams}g)", 
            ingredient.Name, ingredient.Quantity, ingredient.Unit, ingredient.QuantityInGrams);

        var scoredProducts = new List<(PriceComparison Product, double Score, ProductSizeInfo Size)>();

        foreach (var product in products)
        {
            var sizeInfo = ParseProductSize(product.Store); // Store alanında ürün adı var
            if (sizeInfo.SizeInGrams <= 0)
            {
                // Gramaj bulunamadıysa, ürün adından dene
                sizeInfo = ParseProductSize(product.MerchantName ?? "");
            }

            var score = CalculateProductScore(ingredient, product, sizeInfo);
            scoredProducts.Add((product, score, sizeInfo));
            
            _logger.LogDebug("  📦 {Store}: {Price} TL, {Size}g, Skor: {Score:F2}", 
                product.Store, product.Price, sizeInfo.SizeInGrams, score);
        }

        // En yüksek skorlu ürünü seç
        var best = scoredProducts.OrderByDescending(x => x.Score).FirstOrDefault();
        
        if (best.Product != null)
        {
            _logger.LogInformation("✅ Seçilen: {Store} - {Price} TL (Skor: {Score:F2})", 
                best.Product.Store, best.Product.Price, best.Score);
        }

        return best.Product;
    }

    public ProductSizeInfo ParseProductSize(string productName)
    {
        var info = new ProductSizeInfo { OriginalName = productName };
        
        if (string.IsNullOrWhiteSpace(productName)) return info;

        // Gramaj pattern'leri
        var patterns = new[]
        {
            // "500 g", "500g", "500 gr", "500gr"
            (@"(\d+(?:[.,]\d+)?)\s*(g|gr|gram)\b", 1.0),
            // "1 kg", "1kg", "1.5 kg"
            (@"(\d+(?:[.,]\d+)?)\s*(kg|kilo)\b", 1000.0),
            // "500 ml", "1 lt", "1 litre"
            (@"(\d+(?:[.,]\d+)?)\s*(ml)\b", 1.0),
            (@"(\d+(?:[.,]\d+)?)\s*(lt|litre|l)\b", 1000.0),
            // "6'lı", "12'li" (adet)
            (@"(\d+)\s*['']?\s*l[ıi]\b", 0.0), // Adet için özel işlem
            // "x6", "x12"
            (@"x\s*(\d+)\b", 0.0),
        };

        foreach (var (pattern, multiplier) in patterns)
        {
            var match = Regex.Match(productName, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var value = double.Parse(match.Groups[1].Value.Replace(",", "."));
                if (multiplier > 0)
                {
                    info.SizeInGrams = value * multiplier;
                    info.Unit = multiplier == 1000 ? "kg" : "g";
                }
                else
                {
                    info.PackCount = (int)value;
                    info.Unit = "adet";
                }
                break;
            }
        }

        return info;
    }

    private double CalculateProductScore(IngredientInfo ingredient, PriceComparison product, ProductSizeInfo sizeInfo)
    {
        double score = 100;
        
        // 1. Birim fiyat hesapla (TL/kg veya TL/L)
        double unitPricePerKg = 0;
        if (sizeInfo.SizeInGrams > 0)
        {
            unitPricePerKg = (product.Price / sizeInfo.SizeInGrams) * 1000; // TL/kg
        }
        
        // 2. Miktar uygunluk skoru (40% ağırlık)
        double quantityScore = 0;
        if (sizeInfo.SizeInGrams > 0 && ingredient.QuantityInGrams > 0)
        {
            var ratio = sizeInfo.SizeInGrams / ingredient.QuantityInGrams;
            
            // İdeal: İhtiyacın 0.8-2.5 katı arası
            if (ratio >= 0.8 && ratio <= 2.5)
            {
                quantityScore = 40; // Mükemmel uyum
            }
            else if (ratio >= 0.5 && ratio <= 4)
            {
                quantityScore = 30; // İyi uyum
            }
            else if (ratio >= 0.3 && ratio <= 6)
            {
                quantityScore = 15; // Kabul edilebilir
            }
            else if (ratio > 6)
            {
                quantityScore = -20; // Çok fazla (israf)
            }
            else if (ratio < 0.3)
            {
                quantityScore = -10; // Yetersiz
            }
        }
        score += quantityScore;

        // 3. Fiyat uygunluk skoru (30% ağırlık)
        double priceScore = 0;
        if (unitPricePerKg > 0)
        {
            // Birim fiyata göre skorla (0-100 TL/kg arası)
            if (unitPricePerKg < 20)
            {
                priceScore = 30; // Çok ekonomik
            }
            else if (unitPricePerKg < 50)
            {
                priceScore = 25; // Ekonomik
            }
            else if (unitPricePerKg < 100)
            {
                priceScore = 15; // Normal
            }
            else if (unitPricePerKg < 200)
            {
                priceScore = 5; // Pahalı
            }
            else
            {
                priceScore = -10; // Çok pahalı
            }
        }
        else
        {
            // Birim fiyat yoksa toplam fiyata bak
            if (product.Price < 30)
            {
                priceScore = 25;
            }
            else if (product.Price < 60)
            {
                priceScore = 15;
            }
            else if (product.Price < 100)
            {
                priceScore = 5;
            }
            else
            {
                priceScore = -10;
            }
        }
        score += priceScore;

        // 4. İndirim bonusu (20% ağırlık)
        if (product.IsOnSale && product.DiscountPercentage.HasValue && product.DiscountPercentage.Value > 0)
        {
            // İndirim yüzdesine göre bonus
            var discountBonus = Math.Min(20, product.DiscountPercentage.Value / 2.0); // Max 20 puan
            score += discountBonus;
            
            _logger.LogDebug("🏷️ İndirim bonusu: {Product} - %{Discount} indirim = +{Bonus} puan", 
                product.Store, product.DiscountPercentage.Value, discountBonus);
        }

        // 5. İsim benzerliği skoru (10% ağırlık)
        var nameSimilarity = CalculateNameSimilarity(ingredient.Name, product.Store);
        score += nameSimilarity * 10;

        // 6. Çok pahalı ürünleri cezalandır (toplam fiyat)
        if (product.Price > 300)
        {
            score -= 40;
        }
        else if (product.Price > 200)
        {
            score -= 25;
        }
        else if (product.Price > 150)
        {
            score -= 10;
        }

        // 7. Optimal fiyat aralığına bonus
        if (product.Price >= 10 && product.Price <= 80)
        {
            score += 10; // Makul fiyat aralığı
        }

        _logger.LogDebug("📊 Skor: {Product} = {Score} (Miktar:{QScore}, Fiyat:{PScore}, Birim:{UnitPrice} TL/kg)", 
            product.Store, Math.Round(score, 2), Math.Round(quantityScore, 2), 
            Math.Round(priceScore, 2), Math.Round(unitPricePerKg, 2));

        return score;
    }

    /// <summary>
    /// İki string arasındaki benzerliği hesaplar (0-1 arası)
    /// </summary>
    private double CalculateNameSimilarity(string name1, string name2)
    {
        if (string.IsNullOrEmpty(name1) || string.IsNullOrEmpty(name2))
            return 0;

        var lower1 = name1.ToLower();
        var lower2 = name2.ToLower();

        // Tam eşleşme
        if (lower1 == lower2)
            return 1.0;

        // Birbirini içerme
        if (lower2.Contains(lower1) || lower1.Contains(lower2))
            return 0.8;

        // Kelime bazında eşleşme
        var words1 = lower1.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        var words2 = lower2.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

        int matchCount = words1.Count(w1 => words2.Any(w2 => w2.Contains(w1) || w1.Contains(w2)));
        
        if (matchCount > 0)
        {
            return (double)matchCount / Math.Max(words1.Length, words2.Length);
        }

        return 0;
    }

    private string NormalizeUnit(string unit)
    {
        var lower = unit.ToLower().Trim();
        
        return lower switch
        {
            "gram" or "gr" or "g" => "g",
            "kg" or "kilo" or "kilogram" => "kg",
            "ml" or "mililitre" => "ml",
            "lt" or "l" or "litre" => "lt",
            "adet" or "tane" => "adet",
            "su bardağı" or "bardak" => "bardak",
            "yemek kaşığı" or "kaşık" => "yemek kaşığı",
            "çay kaşığı" => "çay kaşığı",
            "tutam" or "çimdik" => "tutam",
            "diş" => "diş",
            "demet" or "dal" => "demet",
            _ => lower
        };
    }

    private double ConvertToGrams(double quantity, string unit, string ingredientName)
    {
        // Yaklaşık gram değerleri
        return unit switch
        {
            "g" => quantity,
            "kg" => quantity * 1000,
            "ml" => quantity, // Sıvılar için yaklaşık
            "lt" => quantity * 1000,
            "adet" => EstimateAdetGrams(ingredientName, quantity),
            "bardak" => quantity * 200, // 1 su bardağı ≈ 200g
            "yemek kaşığı" => quantity * 15,
            "çay kaşığı" => quantity * 5,
            "tutam" => quantity * 2,
            "diş" => quantity * 5, // 1 diş sarımsak ≈ 5g
            "demet" => quantity * 30, // 1 demet ≈ 30g
            _ => quantity * 100 // Varsayılan
        };
    }

    private double EstimateAdetGrams(string name, double quantity)
    {
        var lower = name.ToLower();
        
        // Yaygın ürünlerin yaklaşık ağırlıkları
        var weights = new Dictionary<string, double>
        {
            { "yumurta", 60 },
            { "domates", 150 },
            { "soğan", 150 },
            { "patates", 200 },
            { "havuç", 100 },
            { "salatalık", 200 },
            { "biber", 100 },
            { "limon", 100 },
            { "elma", 180 },
            { "muz", 120 },
            { "portakal", 200 },
            { "sarımsak", 40 }, // Bütün sarımsak
            { "kabak", 300 },
            { "patlıcan", 300 },
        };

        foreach (var (key, weight) in weights)
        {
            if (lower.Contains(key))
            {
                return quantity * weight;
            }
        }

        return quantity * 100; // Varsayılan
    }

    private string CleanIngredientName(string name)
    {
        // Miktarları ve birimleri çıkar
        var removePatterns = new[]
        {
            @"^\d+(?:[.,]\d+)?\s*",
            @"\b(gram|gr|g|kg|kilo|ml|litre|lt|adet|tane|bardak|kaşık|tutam|diş|demet|dal)\b",
            @"\b(su|çay|yemek|tatlı)\s+(bardağı|kaşığı)\b",
            @"\b(yarım|çeyrek|buçuk)\b",
        };

        var result = name;
        foreach (var pattern in removePatterns)
        {
            result = Regex.Replace(result, pattern, "", RegexOptions.IgnoreCase);
        }

        return result.Trim();
    }
}

public class IngredientInfo
{
    public string OriginalText { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public double QuantityInGrams { get; set; }
}

public class ProductSizeInfo
{
    public string OriginalName { get; set; } = string.Empty;
    public double SizeInGrams { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int PackCount { get; set; } = 1;
}
