using SmartShopper.Api.Models;
using System.Text.Json;

namespace SmartShopper.Api.Services;

/// <summary>
/// AI destekli akıllı ürün seçim servisi
/// </summary>
public interface IAIProductSelectorService
{
    /// <summary>
    /// AI kullanarak en uygun ürünü seçer
    /// </summary>
    Task<CimriProduct?> SelectBestProductAsync(string ingredientName, string quantity, List<CimriProduct> products);
    
    /// <summary>
    /// Ürünün malzeme ile eşleşip eşleşmediğini kontrol eder
    /// </summary>
    Task<bool> IsProductRelevantAsync(string ingredientName, string productName);
}

public class AIProductSelectorService : IAIProductSelectorService
{
    private readonly GroqApiService _groqService;
    private readonly ILogger<AIProductSelectorService> _logger;

    public AIProductSelectorService(GroqApiService groqService, ILogger<AIProductSelectorService> logger)
    {
        _groqService = groqService;
        _logger = logger;
    }

    public async Task<CimriProduct?> SelectBestProductAsync(string ingredientName, string quantity, List<CimriProduct> products)
    {
        if (!products.Any())
            return null;

        // Sadece ilk 10 ürünü AI'a gönder (token tasarrufu)
        var topProducts = products.Take(10).ToList();
        
        try
        {
            var productList = string.Join("\n", topProducts.Select((p, i) => 
            {
                var saleInfo = p.IsOnSale ? $" [İNDİRİM: %{p.DiscountPercentage}]" : "";
                return $"{i + 1}. {p.Name} - {p.Price} TL ({p.MerchantName}){saleInfo}";
            }));

            var prompt = $@"Sen bir market alışverişi uzmanısın. Bir tarif için malzeme arıyorum.

ARANAN MALZEME: {ingredientName}
İHTİYAÇ MİKTARI: {quantity}

BULUNAN ÜRÜNLER:
{productList}

GÖREV: Bu ürünlerden tarif için EN UYGUN olanı seç.

⚠️ KRİTİK KURAL: ÜRÜN ADI ARANAN MALZEME İLE TAM EŞLEŞMELI!

SEÇIM KRİTERLERİ (ÖNCELİK SIRASINA GÖRE):
1. **ÜRÜN ADI KONTROLÜ** (EN ÖNEMLİ!):
   - Ürün adında ARANAN MALZEME kelimesi GEÇMELİ
   - ""zeytinyağı"" için ""kantaron yağı"" YANLIŞ ❌ (farklı ürün!)
   - ""zeytinyağı"" için ""riviera zeytinyağı"" DOĞRU ✅
   - ""pirinç"" için ""pirinç patlağı çikolata"" YANLIŞ ❌ (alakasız)
   - ""pirinç"" için ""baldo pirinç 1kg"" DOĞRU ✅
   - ""tavuk"" için ""tavuk aromalı cips"" YANLIŞ ❌ (aromalı ürün)
   - ""tavuk"" için ""tavuk göğsü"" DOĞRU ✅
   - ""makarna"" için ""makarna sosu"" YANLIŞ ❌ (sos, makarna değil)
   - ""makarna"" için ""spagetti makarna"" DOĞRU ✅

2. MİKTAR UYGUNLUĞU: Gramaj/miktar ihtiyaca YAKIN olmalı
   - 30g zeytinyağı için 100ml şişe UYGUN ✅
   - 30g zeytinyağı için 5L bidon UYGUN DEĞİL ❌ (çok fazla)
   - 200g makarna için 500g paket UYGUN ✅
   - 1 litre süt için 1lt süt MÜKEMMEL ✅

3. FİYAT: Makul fiyatlı olmalı (çok pahalı olmamalı)

4. KATEGORİ: Market gıda ürünü olmalı

⚠️ UYARI: Eğer ürün adında aranan malzeme kelimesi YOKSA, o ürünü ASLA seçme!

SADECE şu formatta JSON döndür:
{{
  ""selectedIndex"": 1,
  ""reason"": ""Seçim sebebi kısa açıklama"",
  ""isRelevant"": true
}}

Eğer HİÇBİR ürün uygun değilse (ürün adı eşleşmiyor veya alakasız):
{{
  ""selectedIndex"": 0,
  ""reason"": ""Neden uygun ürün yok (örn: ürün adları eşleşmiyor)"",
  ""isRelevant"": false
}}

SADECE JSON döndür, başka metin ekleme.";;

            var response = await _groqService.GenerateContentAsync(prompt);
            
            // JSON'u parse et
            var jsonStart = response.IndexOf("{");
            var jsonEnd = response.LastIndexOf("}") + 1;
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = response.Substring(jsonStart, jsonEnd - jsonStart);
                var result = JsonSerializer.Deserialize<AIProductSelectionResult>(jsonStr, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                if (result != null)
                {
                    _logger.LogInformation("🤖 AI Ürün Seçimi: {Ingredient} -> Index: {Index}, Uygun: {Relevant}, Sebep: {Reason}", 
                        ingredientName, result.SelectedIndex, result.IsRelevant, result.Reason);
                    
                    if (result.IsRelevant && result.SelectedIndex > 0 && result.SelectedIndex <= topProducts.Count)
                    {
                        return topProducts[result.SelectedIndex - 1];
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI ürün seçimi başarısız, fallback kullanılacak: {Ingredient}", ingredientName);
        }

        // AI başarısız olursa basit filtreleme yap
        return FallbackProductSelection(ingredientName, products);
    }

    public async Task<bool> IsProductRelevantAsync(string ingredientName, string productName)
    {
        try
        {
            var prompt = $@"Bir tarif malzemesi ile market ürününün eşleşip eşleşmediğini kontrol et.

MALZEME: {ingredientName}
ÜRÜN: {productName}

SORU: Bu ürün, tarif için gereken malzeme olarak kullanılabilir mi?

KURALLAR:
- ""pirinç"" malzemesi için ""pirinç patlağı çikolata"" HAYIR (alakasız)
- ""pirinç"" malzemesi için ""baldo pirinç 1kg"" EVET (doğru ürün)
- ""tavuk"" malzemesi için ""tavuk aromalı cips"" HAYIR (alakasız)
- ""tavuk"" malzemesi için ""tavuk göğsü"" EVET (doğru ürün)
- ""makarna"" malzemesi için ""makarna sosu"" HAYIR (farklı ürün)
- ""makarna"" malzemesi için ""spagetti makarna"" EVET (doğru ürün)

SADECE ""EVET"" veya ""HAYIR"" yaz.";

            var response = await _groqService.GenerateContentAsync(prompt);
            var isRelevant = response.Trim().ToUpper().Contains("EVET");
            
            _logger.LogDebug("🤖 AI Ürün Kontrolü: {Ingredient} <-> {Product} = {Result}", 
                ingredientName, productName, isRelevant ? "UYGUN" : "UYGUN DEĞİL");
            
            return isRelevant;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI ürün kontrolü başarısız: {Ingredient} <-> {Product}", ingredientName, productName);
            // Fallback: basit string kontrolü
            return productName.ToLower().Contains(ingredientName.ToLower());
        }
    }

    private CimriProduct? FallbackProductSelection(string ingredientName, List<CimriProduct> products)
    {
        var ingredientLower = ingredientName.ToLower();
        
        // Alakasız kelimeleri içeren ürünleri filtrele
        var excludeKeywords = new[] { "çikolata", "gofret", "bisküvi", "cips", "kraker", "aromalı", "patlak", "bar" };
        
        var filtered = products
            .Where(p => !excludeKeywords.Any(k => p.Name.ToLower().Contains(k)))
            .Where(p => p.Name.ToLower().Contains(ingredientLower) || 
                       ingredientLower.Split(' ').Any(word => p.Name.ToLower().Contains(word)))
            .OrderBy(p => p.Price)
            .ToList();

        return filtered.FirstOrDefault() ?? products.FirstOrDefault();
    }
}

public class AIProductSelectionResult
{
    public int SelectedIndex { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsRelevant { get; set; }
    public bool IsValid { get; set; } // SmartProductMatchingService için
}
