using SmartShopper.Api.Models;
using System.Text.Json;
using HtmlAgilityPack;
using System.Text;
using System.Web;

namespace SmartShopper.Api.Services;

public class PriceComparisonService : IPriceComparisonService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PriceComparisonService> _logger;
    private readonly PriceCacheService _cacheService;
    private readonly ICimriScraperService _cimriScraperService;
    private readonly IAIProductSelectorService _aiProductSelector;
    private readonly ISmartProductMatchingService _smartProductMatching;

    public PriceComparisonService(
        HttpClient httpClient, 
        ILogger<PriceComparisonService> logger, 
        PriceCacheService cacheService,
        ICimriScraperService cimriScraperService,
        IAIProductSelectorService aiProductSelector,
        ISmartProductMatchingService smartProductMatching)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cacheService = cacheService;
        _cimriScraperService = cimriScraperService;
        _aiProductSelector = aiProductSelector;
        _smartProductMatching = smartProductMatching;
    }

    public async Task<List<PriceComparison>> ComparePricesAsync(string productName, string? quantity = null)
    {
        // Önce cache'den kontrol et
        if (_cacheService.TryGetCachedPrices(productName, out var cachedPrices))
        {
            _logger.LogInformation("Cache'den fiyat döndürülüyor: {ProductName}", productName);
            return cachedPrices;
        }

        try
        {
            _logger.LogInformation("Cimri.com'dan gerçek fiyat karşılaştırması başlatılıyor: {ProductName}", productName);
            
            // Sadece Cimri'den gerçek fiyatları al
            var cimriPrices = await GetPricesFromCimriAsync(productName, quantity);
            
            if (!cimriPrices.Any())
            {
                _logger.LogWarning("Cimri.com'da ürün bulunamadı: {ProductName}", productName);
                return new List<PriceComparison>();
            }
            
            // Fiyata göre sırala
            var sortedPrices = cimriPrices.OrderBy(p => p.Price).ToList();
            
            // Cache'e kaydet
            _cacheService.CachePrices(productName, sortedPrices);
            
            _logger.LogInformation("Fiyat karşılaştırması tamamlandı. {Count} sonuç bulundu: {ProductName}", 
                sortedPrices.Count, productName);
            
            return sortedPrices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fiyat karşılaştırması sırasında hata oluştu: {ProductName}", productName);
            return new List<PriceComparison>();
        }
    }

    public async Task<List<PriceComparison>> GetPricesFromCimriAsync(string productName, string? quantity = null)
    {
        try
        {
            _logger.LogInformation("🎯 Akıllı Ürün Eşleştirme Başlatılıyor: {ProductName} (Miktar: {Quantity})", 
                productName, quantity ?? "belirtilmedi");
            
            // YENİ: 3 Aşamalı Akıllı Eşleştirme Kullan
            // Adım 1: Query Expansion (Gemini ile arama terimini zenginleştir)
            // Adım 2: Multi-Result Scraping (İlk 5 ürünü çek)
            // Adım 3: AI Re-ranking (Gemini ile en doğrusunu seç)
            var bestProduct = await _smartProductMatching.FindBestMatchAsync(productName, quantity);
            
            if (bestProduct == null)
            {
                _logger.LogWarning("❌ Akıllı eşleştirme ürün bulamadı: {ProductName}", productName);
                return new List<PriceComparison>();
            }
            
            _logger.LogInformation("✅ En İyi Eşleşme Bulundu: {Product} - {Price} TL", 
                bestProduct.Name, bestProduct.Price);
            
            if (bestProduct.Price <= 0)
            {
                _logger.LogWarning("Geçerli fiyatlı ürün bulunamadı: {ProductName}", productName);
                return new List<PriceComparison>();
            }
            
            var prices = new List<PriceComparison>();
            
            // Ürün detaylarını al (farklı marketlerdeki fiyatları görmek için)
            try
            {
                var productDetails = await _cimriScraperService.GetProductDetailsAsync(bestProduct.Id);
                
                if (productDetails?.Offers != null && productDetails.Offers.Any())
                {
                    // Farklı marketlerdeki fiyatları ekle
                    foreach (var offer in productDetails.Offers.Take(10)) // İlk 10 market
                    {
                        if (offer.Price <= 0)
                            continue;
                        
                        prices.Add(new PriceComparison
                        {
                            Store = offer.MerchantName,
                            Price = (double)offer.Price,
                            Currency = "TL",
                            IsAvailable = true,
                            LastUpdated = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                            ProductUrl = bestProduct.ProductUrl,
                            ImageUrl = bestProduct.ImageUrl,
                            UnitPrice = offer.UnitPrice.HasValue ? (double?)offer.UnitPrice.Value : null,
                            IsOnSale = bestProduct.IsOnSale,
                            OriginalPrice = bestProduct.OriginalPrice.HasValue ? (double?)bestProduct.OriginalPrice.Value : null,
                            DiscountPercentage = bestProduct.DiscountPercentage
                        });
                    }
                    
                    _logger.LogInformation("✅ {Count} farklı market fiyatı bulundu: {ProductName}", 
                        prices.Count, productName);
                }
                else
                {
                    // Detay alınamazsa sadece ilk ürünün fiyatını ekle
                    prices.Add(new PriceComparison
                    {
                        Store = bestProduct.MerchantName,
                        Price = (double)bestProduct.Price,
                        Currency = "TL",
                        IsAvailable = true,
                        LastUpdated = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                        ProductUrl = bestProduct.ProductUrl,
                        ImageUrl = bestProduct.ImageUrl,
                        IsOnSale = bestProduct.IsOnSale,
                        OriginalPrice = bestProduct.OriginalPrice.HasValue ? (double?)bestProduct.OriginalPrice.Value : null,
                        DiscountPercentage = bestProduct.DiscountPercentage
                    });
                    
                    _logger.LogInformation("✅ Tek fiyat bulundu: {ProductName} - {Price} TL ({Store}){Sale}", 
                        productName, bestProduct.Price, bestProduct.MerchantName,
                        bestProduct.IsOnSale ? " [İNDİRİMDE]" : "");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ürün detayları alınamadı, sadece ilk fiyat kullanılıyor: {ProductName}", productName);
                
                // Hata durumunda sadece ilk ürünün fiyatını ekle
                prices.Add(new PriceComparison
                {
                    Store = bestProduct.MerchantName,
                    Price = (double)bestProduct.Price,
                    Currency = "TL",
                    IsAvailable = true,
                    LastUpdated = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                    ProductUrl = bestProduct.ProductUrl,
                    ImageUrl = bestProduct.ImageUrl,
                    IsOnSale = bestProduct.IsOnSale,
                    OriginalPrice = bestProduct.OriginalPrice.HasValue ? (double?)bestProduct.OriginalPrice.Value : null,
                    DiscountPercentage = bestProduct.DiscountPercentage
                });
            }
            
            return prices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cimri.com'dan fiyat alınamadı: {ProductName}", productName);
            return new List<PriceComparison>();
        }
    }

    public async Task<List<PriceComparison>> GetPricesFromMigrosAsync(string productName)
    {
        // TODO: Gerçek Migros scraper implementasyonu
        _logger.LogInformation("Migros scraper henüz implement edilmedi: {ProductName}", productName);
        return new List<PriceComparison>();
    }

    public async Task<List<PriceComparison>> GetPricesFromCarrefourAsync(string productName)
    {
        // TODO: Gerçek CarrefourSA scraper implementasyonu
        _logger.LogInformation("CarrefourSA scraper henüz implement edilmedi: {ProductName}", productName);
        return new List<PriceComparison>();
    }

    public async Task<List<PriceComparison>> GetPricesFromBimAsync(string productName)
    {
        // TODO: Gerçek BIM scraper implementasyonu
        _logger.LogInformation("BIM scraper henüz implement edilmedi: {ProductName}", productName);
        return new List<PriceComparison>();
    }

    public async Task<List<PriceComparison>> GetPricesFromA101Async(string productName)
    {
        // TODO: Gerçek A101 scraper implementasyonu
        _logger.LogInformation("A101 scraper henüz implement edilmedi: {ProductName}", productName);
        return new List<PriceComparison>();
    }

    private bool TryParsePrice(string priceText, out double price)
    {
        price = 0;
        
        if (string.IsNullOrWhiteSpace(priceText))
            return false;
        
        // Fiyat metnini temizle
        var cleanPrice = priceText
            .Replace("TL", "")
            .Replace("₺", "")
            .Replace(".", "") // Binlik ayırıcı
            .Replace(",", ".") // Ondalık ayırıcı
            .Trim();
        
        // Sadece sayıları ve ondalık noktayı bırak
        var numericPrice = new StringBuilder();
        bool hasDecimal = false;
        
        foreach (char c in cleanPrice)
        {
            if (char.IsDigit(c))
            {
                numericPrice.Append(c);
            }
            else if (c == '.' && !hasDecimal)
            {
                numericPrice.Append(c);
                hasDecimal = true;
            }
        }
        
        return double.TryParse(numericPrice.ToString(), out price);
    }

    /// <summary>
    /// Arama sonuçlarından alakalı ürünleri filtreler
    /// </summary>
    private List<CimriProduct> FilterRelevantProducts(List<CimriProduct> products, string searchTerm)
    {
        var searchLower = searchTerm.ToLower().Trim();
        var searchWords = searchLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Alakasız kategoriler/kelimeler
        var excludeKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "çikolata", "gofret", "bisküvi", "şekerleme", "cips", "kraker",
            "deterjan", "temizlik", "şampuan", "sabun", "kozmetik",
            "oyuncak", "elektronik", "tekstil", "giyim",
            "patlak", "patlağı", "patlakli", // pirinç patlağı gibi
            "bar", "tablet", // çikolata bar gibi
            "aromalı", "aromali" // aromalı ürünler
        };
        
        // Gıda kategorisi için kabul edilen kelimeler
        var foodKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "kg", "gr", "g", "lt", "ml", "litre", "adet",
            "paket", "kutu", "şişe", "poşet", "torba"
        };
        
        var relevantProducts = new List<CimriProduct>();
        
        foreach (var product in products)
        {
            var productNameLower = product.Name.ToLower();
            
            // Alakasız kelimeleri içeren ürünleri atla
            bool isExcluded = excludeKeywords.Any(keyword => productNameLower.Contains(keyword));
            if (isExcluded)
            {
                _logger.LogDebug("Ürün filtrelendi (alakasız): {ProductName}", product.Name);
                continue;
            }
            
            // Arama teriminin ürün adında geçip geçmediğini kontrol et
            bool containsSearchTerm = searchWords.All(word => productNameLower.Contains(word));
            
            // Veya ürün adı arama terimini içeriyor mu
            bool productContainsSearch = productNameLower.Contains(searchLower);
            
            if (containsSearchTerm || productContainsSearch)
            {
                relevantProducts.Add(product);
                _logger.LogDebug("Alakalı ürün bulundu: {ProductName}", product.Name);
            }
        }
        
        // Eğer hiç alakalı ürün bulunamadıysa, en azından ilk birkaç ürünü döndür
        // ama yine de alakasız olanları filtrele
        if (!relevantProducts.Any())
        {
            relevantProducts = products
                .Where(p => !excludeKeywords.Any(k => p.Name.ToLower().Contains(k)))
                .Take(5)
                .ToList();
        }
        
        _logger.LogInformation("Filtreleme sonucu: {Original} -> {Filtered} ürün ({SearchTerm})", 
            products.Count, relevantProducts.Count, searchTerm);
        
        return relevantProducts;
    }
}