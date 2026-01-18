using SmartShopper.Api.Models;
using System.Text.Json;

namespace SmartShopper.Api.Services;

/// <summary>
/// 3 Aşamalı Akıllı Ürün Eşleştirme Servisi
/// Adım 1: Query Expansion (Arama terimini zenginleştir)
/// Adım 2: Multi-Result Scraping (İlk 5 ürünü çek)
/// Adım 3: AI Re-ranking (En doğrusunu seç)
/// </summary>
public interface ISmartProductMatchingService
{
    /// <summary>
    /// Akıllı ürün eşleştirme ile en uygun ürünü bulur
    /// </summary>
    Task<CimriProduct?> FindBestMatchAsync(string productName, string? quantity = null);
    
    /// <summary>
    /// Arama terimini AI ile zenginleştirir
    /// </summary>
    Task<string> ExpandSearchQueryAsync(string productName);
}

public class SmartProductMatchingService : ISmartProductMatchingService
{
    private readonly ICimriScraperService _scraperService;
    private readonly GeminiApiService _geminiService;
    private readonly ILogger<SmartProductMatchingService> _logger;

    public SmartProductMatchingService(
        ICimriScraperService scraperService,
        GeminiApiService geminiService,
        ILogger<SmartProductMatchingService> logger)
    {
        _scraperService = scraperService;
        _geminiService = geminiService;
        _logger = logger;
    }

    public async Task<CimriProduct?> FindBestMatchAsync(string productName, string? quantity = null)
    {
        try
        {
            _logger.LogInformation("🎯 Akıllı Eşleştirme Başladı: {Product}", productName);

            // ADIM 1: Query Expansion - Arama terimini zenginleştir
            var expandedQuery = await ExpandSearchQueryAsync(productName);
            _logger.LogInformation("📝 Zenginleştirilmiş Arama: '{Original}' -> '{Expanded}'", 
                productName, expandedQuery);

            // ADIM 2: Multi-Result Scraping - İlk 5 ürünü çek
            var searchResult = await _scraperService.SearchProductsAsync(expandedQuery, page: 1, sort: "price-asc");
            
            if (searchResult == null || !searchResult.Products.Any())
            {
                _logger.LogWarning("❌ Ürün bulunamadı: {Query}", expandedQuery);
                return null;
            }

            var topProducts = searchResult.Products.Take(5).ToList();
            _logger.LogInformation("🔍 {Count} ürün bulundu, AI ile en iyisi seçiliyor...", topProducts.Count);

            // ADIM 3: AI Re-ranking - Gemini ile en doğrusunu seç
            var bestProduct = await SelectBestProductWithAI(productName, quantity, topProducts);

            if (bestProduct != null)
            {
                _logger.LogInformation("✅ En İyi Eşleşme: {Product} - {Price} TL ({Store})", 
                    bestProduct.Name, bestProduct.Price, bestProduct.MerchantName);
            }
            else
            {
                _logger.LogWarning("⚠️ AI uygun ürün bulamadı, fallback kullanılıyor");
                bestProduct = FallbackSelection(productName, topProducts);
            }

            return bestProduct;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Akıllı eşleştirme hatası: {Product}", productName);
            return null;
        }
    }

    public async Task<string> ExpandSearchQueryAsync(string productName)
    {
        try
        {
            var prompt = $@"Sen bir online market arama uzmanısın. Kullanıcının alışveriş listesindeki ürün adını, Cimri.com'da aratmak için en uygun ve genel geçer ürün adına çevir.

KULLANICI GİRDİSİ: ""{productName}""

KURALLAR:
1. Eğer sadece genel bir kategori verilmişse (örn: ""Peynir""), en yaygın türünü ekle (örn: ""Beyaz Peynir"")
2. Eğer marka verilmemişse, marka ekleme (genel ürün adı kullan)
3. Eğer miktar verilmemişse, standart market gramajını ekle (örn: 500g, 1kg, 1L)
4. Türkçe karakterleri koru
5. Gereksiz kelimeler ekleme, sadece ürün adını optimize et
6. Gıda kategorisinde kal, alakasız ürünlere yönlendirme

ÖRNEKLER:
- ""Peynir"" -> ""Beyaz Peynir 500g""
- ""Süt"" -> ""Süt 1L""
- ""Makarna"" -> ""Makarna 500g""
- ""Zeytinyağı"" -> ""Zeytinyağı 1L""
- ""Domates"" -> ""Domates 1kg""
- ""Yumurta"" -> ""Yumurta 10'lu""
- ""Ekmek"" -> ""Ekmek""

SADECE optimize edilmiş ürün adını döndür, başka açıklama ekleme.";

            var response = await _geminiService.GenerateContentAsync(prompt);
            var expandedQuery = response.Trim().Trim('"');

            // Eğer AI boş döndüyse veya çok uzunsa, orijinali kullan
            if (string.IsNullOrWhiteSpace(expandedQuery) || expandedQuery.Length > 100)
            {
                _logger.LogWarning("AI query expansion başarısız, orijinal kullanılıyor");
                return productName;
            }

            return expandedQuery;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Query expansion hatası, orijinal kullanılıyor");
            return productName;
        }
    }

    private async Task<CimriProduct?> SelectBestProductWithAI(
        string originalProductName, 
        string? quantity, 
        List<CimriProduct> products)
    {
        try
        {
            var productList = string.Join("\n", products.Select((p, i) => 
                $"{i + 1}. {p.Name} - {p.Price} TL - {p.MerchantName ?? "Bilinmeyen"}"));

            var prompt = $@"Sen bir market alışverişi uzmanısın. Kullanıcı ""{originalProductName}"" arıyor.

Aşağıdaki bulunan ürün listesinden, kullanıcının isteğini EN İYİ karşılayan ve GERÇEK GIDA ÜRÜNÜ olan tek bir tanesini seç.

⚠️ KRİTİK KURALLAR:
1. **ÜRÜN TÜRÜ KONTROLÜ** (EN ÖNEMLİ!):
   - Cips, kraker, çikolata, gofret, bisküvi gibi atıştırmalık SEÇME ❌
   - Kozmetik, temizlik, aksesuar ürünleri SEÇME ❌
   - ""Aromalı"" ürünler SEÇME ❌ (örn: ""tavuk aromalı cips"")
   - Sadece ANA GIDA ÜRÜNÜNÜ seç ✅

2. **ÜRÜN ADI EŞLEŞMESİ**:
   - ""Süt"" için ""Vücut Sütü"" YANLIŞ ❌
   - ""Süt"" için ""İçim Süt 1L"" DOĞRU ✅
   - ""Peynir"" için ""Peynir Aromalı Cips"" YANLIŞ ❌
   - ""Peynir"" için ""Beyaz Peynir 500g"" DOĞRU ✅

3. **FİYAT/DEĞER**:
   - Makul fiyatlı olanı tercih et
   - Çok pahalı veya şüpheli fiyatları seçme

4. **KATEGORİ**:
   - Süpermarket/Gıda kategorisinde olmalı

BULUNAN ÜRÜNLER:
{productList}

SADECE şu JSON formatında yanıt ver:
{{
  ""selectedIndex"": 1,
  ""reason"": ""Seçim sebebi (kısa)"",
  ""isValid"": true
}}

Eğer HİÇBİR ürün uygun değilse (hepsi alakasız):
{{
  ""selectedIndex"": 0,
  ""reason"": ""Neden uygun değil"",
  ""isValid"": false
}}

SADECE JSON döndür, başka metin ekleme.";

            var response = await _geminiService.GenerateContentAsync(prompt);
            
            // JSON'u parse et
            var jsonStart = response.IndexOf("{");
            var jsonEnd = response.LastIndexOf("}") + 1;
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = response.Substring(jsonStart, jsonEnd - jsonStart);
                var result = JsonSerializer.Deserialize<AIProductSelectionResult>(jsonStr, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (result != null)
                {
                    _logger.LogInformation("🤖 Gemini Seçimi: Index={Index}, Geçerli={Valid}, Sebep={Reason}", 
                        result.SelectedIndex, result.IsValid, result.Reason);
                    
                    if (result.IsValid && result.SelectedIndex > 0 && result.SelectedIndex <= products.Count)
                    {
                        return products[result.SelectedIndex - 1];
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI ürün seçimi başarısız");
        }

        return null;
    }

    private CimriProduct? FallbackSelection(string productName, List<CimriProduct> products)
    {
        _logger.LogInformation("🔄 Fallback seçim algoritması kullanılıyor");

        var productLower = productName.ToLower();
        
        // Alakasız kelimeleri filtrele
        var excludeKeywords = new[] 
        { 
            "çikolata", "gofret", "bisküvi", "cips", "kraker", "aromalı", 
            "patlak", "bar", "kozmetik", "vücut", "cilt", "saç", "temizlik",
            "tabak", "kase", "bardak", "kaşık"
        };
        
        var filtered = products
            .Where(p => !excludeKeywords.Any(k => p.Name.ToLower().Contains(k)))
            .Where(p => 
            {
                var nameLower = p.Name.ToLower();
                // Ürün adında aranan kelime geçmeli
                return nameLower.Contains(productLower) || 
                       productLower.Split(' ').Any(word => word.Length > 2 && nameLower.Contains(word));
            })
            .OrderBy(p => p.Price)
            .ToList();

        var selected = filtered.FirstOrDefault();
        
        if (selected != null)
        {
            _logger.LogInformation("✅ Fallback seçim: {Product}", selected.Name);
        }
        else
        {
            _logger.LogWarning("⚠️ Fallback bile uygun ürün bulamadı");
        }

        return selected;
    }
}
