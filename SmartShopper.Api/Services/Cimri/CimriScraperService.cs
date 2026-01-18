using Microsoft.Extensions.Options;
using SmartShopper.Api.Models;
using System.Text.Json;
using System.Web;

namespace SmartShopper.Api.Services;

/// <summary>
/// Service for scraping product data from Cimri.com
/// </summary>
public class CimriScraperService : ICimriScraperService
{
    private readonly CimriHttpClientService _httpClient;
    private readonly ICimriHtmlParserService _htmlParser;
    private readonly ICacheService _cache;
    private readonly ILogger<CimriScraperService> _logger;
    private readonly CimriScraperOptions _options;
    private readonly ScraperService _playwrightScraper;
    private int _totalPages = 0;

    public CimriScraperService(
        CimriHttpClientService httpClient,
        ICimriHtmlParserService htmlParser,
        ICacheService cache,
        ILogger<CimriScraperService> logger,
        IOptions<CimriScraperOptions> options,
        ScraperService playwrightScraper)
    {
        _httpClient = httpClient;
        _htmlParser = htmlParser;
        _cache = cache;
        _logger = logger;
        _options = options.Value;
        _playwrightScraper = playwrightScraper;
    }

    /// <summary>
    /// Searches for products on Cimri.com
    /// </summary>
    public async Task<CimriSearchResult> SearchProductsAsync(string query, int page = 1, string sort = "")
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Search query cannot be empty", nameof(query));
        }

        if (page < 1)
        {
            throw new ArgumentException("Page number must be greater than 0", nameof(page));
        }

        _logger.LogInformation("Searching Cimri.com for query: {Query}, page: {Page}, sort: {Sort}", 
            query, page, sort);

        // Build search URL
        var url = BuildSearchUrl(query, page, sort);
        
        // Fetch HTML (with cache)
        var html = await FetchHtmlAsync(url);

        if (string.IsNullOrWhiteSpace(html))
        {
            _logger.LogWarning("No HTML content received for query: {Query}", query);
            return new CimriSearchResult
            {
                Query = query,
                CurrentPage = page,
                TotalPages = 0,
                Products = new List<CimriProduct>()
            };
        }

        // Parse product list
        var products = ParseProductList(html);

        // Validate page boundary after we know the total pages
        if (_totalPages > 0 && page > _totalPages)
        {
            _logger.LogWarning("Requested page {Page} exceeds total pages {TotalPages} for query: {Query}", 
                page, _totalPages, query);
            throw new ArgumentOutOfRangeException(nameof(page), 
                $"Page number {page} exceeds total pages {_totalPages}");
        }

        var result = new CimriSearchResult
        {
            Query = query,
            CurrentPage = page,
            TotalPages = _totalPages,
            Products = products,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("Found {Count} products for query: {Query}", products.Count, query);

        return result;
    }

    /// <summary>
    /// Gets detailed information about a specific product
    /// </summary>
    public async Task<CimriProductDetail> GetProductDetailsAsync(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));
        }

        _logger.LogInformation("Fetching product details for ID: {ProductId}", productId);

        // Build product detail URL
        var url = $"https://www.cimri.com/urun/{productId}";

        // Fetch HTML (with cache)
        var html = await FetchHtmlAsync(url);

        if (string.IsNullOrWhiteSpace(html))
        {
            _logger.LogWarning("No HTML content received for product ID: {ProductId}", productId);
            throw new InvalidOperationException($"Failed to fetch product details for ID: {productId}");
        }

        // Parse product details
        var productDetail = ParseProductDetail(html, productId);

        _logger.LogInformation("Successfully fetched product details for ID: {ProductId}", productId);

        return productDetail;
    }

    /// <summary>
    /// Parses product detail information from HTML content
    /// </summary>
    private CimriProductDetail ParseProductDetail(string html, string productId)
    {
        var productDetail = new CimriProductDetail
        {
            Id = productId
        };

        try
        {
            // Try parsing JSON-LD first
            var jsonLdDocs = _htmlParser.ParseJsonLd(html);
            
            if (jsonLdDocs.Any())
            {
                _logger.LogDebug("Parsing product details from JSON-LD data");
                ParseProductDetailFromJsonLd(jsonLdDocs, productDetail);
            }

            // Try parsing __NEXT_DATA__
            var nextData = _htmlParser.ParseNextData(html);
            
            if (nextData != null)
            {
                _logger.LogDebug("Parsing product details from __NEXT_DATA__");
                ParseProductDetailFromNextData(nextData, productDetail);
            }

            // Filter invalid price history entries
            productDetail.PriceHistory = FilterInvalidPriceHistory(productDetail.PriceHistory);

            _logger.LogInformation("Successfully parsed product details. Specs: {SpecCount}, Price History: {HistoryCount}, Offers: {OfferCount}",
                productDetail.Specs.Count, productDetail.PriceHistory.Count, productDetail.Offers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing product detail from HTML for product ID: {ProductId}", productId);
        }

        return productDetail;
    }

    /// <summary>
    /// Parses product details from JSON-LD data
    /// </summary>
    private void ParseProductDetailFromJsonLd(List<JsonDocument> jsonLdDocs, CimriProductDetail productDetail)
    {
        foreach (var doc in jsonLdDocs)
        {
            try
            {
                var root = doc.RootElement;

                // Check if this is a Product
                if (root.TryGetProperty("@type", out var typeElement))
                {
                    var type = typeElement.GetString();

                    if (type == "Product")
                    {
                        // Extract name
                        if (root.TryGetProperty("name", out var nameElement))
                        {
                            productDetail.Name = nameElement.GetString() ?? string.Empty;
                        }

                        // Extract description
                        if (root.TryGetProperty("description", out var descElement))
                        {
                            productDetail.Description = descElement.GetString() ?? string.Empty;
                        }

                        // Extract offers (market offers)
                        if (root.TryGetProperty("offers", out var offersElement))
                        {
                            ParseOffersFromJsonLd(offersElement, productDetail);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing product detail from JSON-LD document");
            }
        }
    }

    /// <summary>
    /// Parses offers from JSON-LD offers element
    /// </summary>
    private void ParseOffersFromJsonLd(JsonElement offersElement, CimriProductDetail productDetail)
    {
        try
        {
            // Offers can be a single object or an array
            if (offersElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var offerElement in offersElement.EnumerateArray())
                {
                    var offer = ParseSingleOfferFromJsonLd(offerElement);
                    if (offer != null)
                    {
                        productDetail.Offers.Add(offer);
                    }
                }
            }
            else if (offersElement.ValueKind == JsonValueKind.Object)
            {
                var offer = ParseSingleOfferFromJsonLd(offersElement);
                if (offer != null)
                {
                    productDetail.Offers.Add(offer);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing offers from JSON-LD");
        }
    }

    /// <summary>
    /// Parses a single offer from JSON-LD element
    /// </summary>
    private MarketOffer? ParseSingleOfferFromJsonLd(JsonElement offerElement)
    {
        try
        {
            var offer = new MarketOffer();

            // Extract price
            if (offerElement.TryGetProperty("price", out var priceElement))
            {
                if (decimal.TryParse(priceElement.GetString(), out var price))
                {
                    offer.Price = price;
                }
                else if (priceElement.TryGetDecimal(out var priceDecimal))
                {
                    offer.Price = priceDecimal;
                }
            }

            // Extract seller/merchant info
            if (offerElement.TryGetProperty("seller", out var sellerElement))
            {
                if (sellerElement.TryGetProperty("name", out var sellerNameElement))
                {
                    offer.MerchantName = sellerNameElement.GetString() ?? string.Empty;
                }
            }

            // Only return offer if it has valid price
            if (offer.Price > 0)
            {
                return offer;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing single offer from JSON-LD");
            return null;
        }
    }

    /// <summary>
    /// Parses product details from __NEXT_DATA__
    /// </summary>
    private void ParseProductDetailFromNextData(JsonDocument nextData, CimriProductDetail productDetail)
    {
        try
        {
            var root = nextData.RootElement;

            // Navigate to product data (structure may vary)
            if (root.TryGetProperty("props", out var props))
            {
                if (props.TryGetProperty("pageProps", out var pageProps))
                {
                    // Try to find product object
                    if (pageProps.TryGetProperty("product", out var productElement))
                    {
                        ParseProductFromNextData(productElement, productDetail);
                    }
                    else if (pageProps.TryGetProperty("productDetail", out var productDetailElement))
                    {
                        ParseProductFromNextData(productDetailElement, productDetail);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing product details from __NEXT_DATA__");
        }
    }

    /// <summary>
    /// Parses product information from __NEXT_DATA__ product element
    /// </summary>
    private void ParseProductFromNextData(JsonElement productElement, CimriProductDetail productDetail)
    {
        try
        {
            // Extract name
            if (productElement.TryGetProperty("name", out var nameElement))
            {
                productDetail.Name = nameElement.GetString() ?? string.Empty;
            }

            // Extract description
            if (productElement.TryGetProperty("description", out var descElement))
            {
                productDetail.Description = descElement.GetString() ?? string.Empty;
            }

            // Extract specs/specifications
            if (productElement.TryGetProperty("specs", out var specsElement) ||
                productElement.TryGetProperty("specifications", out specsElement))
            {
                ParseSpecsFromNextData(specsElement, productDetail);
            }

            // Extract price history
            if (productElement.TryGetProperty("priceHistory", out var priceHistoryElement))
            {
                ParsePriceHistoryFromNextData(priceHistoryElement, productDetail);
            }

            // Extract offers/merchants
            if (productElement.TryGetProperty("offers", out var offersElement) ||
                productElement.TryGetProperty("merchants", out offersElement) ||
                productElement.TryGetProperty("offlineOffers", out offersElement))
            {
                ParseOffersFromNextData(offersElement, productDetail);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing product from __NEXT_DATA__");
        }
    }

    /// <summary>
    /// Parses product specifications from __NEXT_DATA__
    /// </summary>
    private void ParseSpecsFromNextData(JsonElement specsElement, CimriProductDetail productDetail)
    {
        try
        {
            if (specsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var specGroupElement in specsElement.EnumerateArray())
                {
                    var specGroup = new ProductSpec();

                    // Extract group name
                    if (specGroupElement.TryGetProperty("group", out var groupElement) ||
                        specGroupElement.TryGetProperty("name", out groupElement))
                    {
                        specGroup.Group = groupElement.GetString() ?? string.Empty;
                    }

                    // Extract items
                    if (specGroupElement.TryGetProperty("items", out var itemsElement) ||
                        specGroupElement.TryGetProperty("specs", out itemsElement))
                    {
                        if (itemsElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var itemElement in itemsElement.EnumerateArray())
                            {
                                var specItem = new SpecItem();

                                if (itemElement.TryGetProperty("name", out var itemNameElement))
                                {
                                    specItem.Name = itemNameElement.GetString() ?? string.Empty;
                                }

                                if (itemElement.TryGetProperty("value", out var itemValueElement))
                                {
                                    specItem.Value = itemValueElement.GetString() ?? string.Empty;
                                }

                                if (!string.IsNullOrEmpty(specItem.Name))
                                {
                                    specGroup.Items.Add(specItem);
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(specGroup.Group) || specGroup.Items.Any())
                    {
                        productDetail.Specs.Add(specGroup);
                    }
                }
            }
            else if (specsElement.ValueKind == JsonValueKind.Object)
            {
                // Specs might be a flat object with key-value pairs
                var specGroup = new ProductSpec { Group = "General" };

                foreach (var property in specsElement.EnumerateObject())
                {
                    var specItem = new SpecItem
                    {
                        Name = property.Name,
                        Value = property.Value.GetString() ?? string.Empty
                    };
                    specGroup.Items.Add(specItem);
                }

                if (specGroup.Items.Any())
                {
                    productDetail.Specs.Add(specGroup);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing specs from __NEXT_DATA__");
        }
    }

    /// <summary>
    /// Parses price history from __NEXT_DATA__
    /// </summary>
    private void ParsePriceHistoryFromNextData(JsonElement priceHistoryElement, CimriProductDetail productDetail)
    {
        try
        {
            if (priceHistoryElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var historyElement in priceHistoryElement.EnumerateArray())
                {
                    var priceHistory = new PriceHistory();

                    // Extract date
                    if (historyElement.TryGetProperty("date", out var dateElement))
                    {
                        if (DateTime.TryParse(dateElement.GetString(), out var date))
                        {
                            priceHistory.Date = date;
                        }
                    }
                    else if (historyElement.TryGetProperty("timestamp", out var timestampElement))
                    {
                        if (long.TryParse(timestampElement.GetString(), out var timestamp))
                        {
                            priceHistory.Date = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                        }
                        else if (timestampElement.TryGetInt64(out var timestampLong))
                        {
                            priceHistory.Date = DateTimeOffset.FromUnixTimeSeconds(timestampLong).DateTime;
                        }
                    }

                    // Extract price
                    if (historyElement.TryGetProperty("price", out var priceElement))
                    {
                        if (priceElement.TryGetDecimal(out var price))
                        {
                            priceHistory.Price = price;
                        }
                        else if (decimal.TryParse(priceElement.GetString(), out var priceDecimal))
                        {
                            priceHistory.Price = priceDecimal;
                        }
                    }

                    // Only add if we have valid data
                    if (priceHistory.Date != default && priceHistory.Price > 0)
                    {
                        productDetail.PriceHistory.Add(priceHistory);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing price history from __NEXT_DATA__");
        }
    }

    /// <summary>
    /// Parses market offers from __NEXT_DATA__
    /// </summary>
    private void ParseOffersFromNextData(JsonElement offersElement, CimriProductDetail productDetail)
    {
        try
        {
            if (offersElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var offerElement in offersElement.EnumerateArray())
                {
                    var offer = new MarketOffer();

                    // Extract merchant ID
                    if (offerElement.TryGetProperty("merchantId", out var merchantIdElement))
                    {
                        offer.MerchantId = merchantIdElement.GetString() ?? string.Empty;
                    }

                    // Extract merchant name
                    if (offerElement.TryGetProperty("merchantName", out var merchantNameElement) ||
                        offerElement.TryGetProperty("merchant", out merchantNameElement))
                    {
                        if (merchantNameElement.ValueKind == JsonValueKind.String)
                        {
                            offer.MerchantName = merchantNameElement.GetString() ?? string.Empty;
                        }
                        else if (merchantNameElement.ValueKind == JsonValueKind.Object)
                        {
                            if (merchantNameElement.TryGetProperty("name", out var nameElement))
                            {
                                offer.MerchantName = nameElement.GetString() ?? string.Empty;
                            }
                        }
                    }

                    // Extract price
                    if (offerElement.TryGetProperty("price", out var priceElement))
                    {
                        if (priceElement.TryGetDecimal(out var price))
                        {
                            offer.Price = price;
                        }
                        else if (decimal.TryParse(priceElement.GetString(), out var priceDecimal))
                        {
                            offer.Price = priceDecimal;
                        }
                    }

                    // Extract unit price
                    if (offerElement.TryGetProperty("unitPrice", out var unitPriceElement))
                    {
                        if (unitPriceElement.TryGetDecimal(out var unitPrice))
                        {
                            offer.UnitPrice = unitPrice;
                        }
                        else if (decimal.TryParse(unitPriceElement.GetString(), out var unitPriceDecimal))
                        {
                            offer.UnitPrice = unitPriceDecimal;
                        }
                    }

                    // Only add if we have valid price
                    if (offer.Price > 0)
                    {
                        productDetail.Offers.Add(offer);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing offers from __NEXT_DATA__");
        }
    }

    /// <summary>
    /// Filters out invalid price history entries (price <= 0 or null)
    /// </summary>
    private List<PriceHistory> FilterInvalidPriceHistory(List<PriceHistory> priceHistory)
    {
        var filtered = priceHistory.Where(ph => ph.Price > 0).ToList();
        
        var removedCount = priceHistory.Count - filtered.Count;
        if (removedCount > 0)
        {
            _logger.LogDebug("Filtered out {Count} invalid price history entries", removedCount);
        }

        return filtered;
    }

    /// <summary>
    /// Gets the total number of pages from the last search
    /// </summary>
    public int GetTotalPages()
    {
        return _totalPages;
    }

    /// <summary>
    /// Builds the search URL with query parameters
    /// </summary>
    private string BuildSearchUrl(string query, int page, string sort)
    {
        // Normalize Turkish characters
        var normalizedQuery = TurkishCharacterHelper.ConvertToUrlSafe(query);
        
        // URL encode the query
        var encodedQuery = HttpUtility.UrlEncode(normalizedQuery);

        // Build base URL
        var url = $"{_options.BaseUrl}?q={encodedQuery}";

        // Add page parameter if not first page
        if (page > 1)
        {
            url += $"&page={page}";
        }

        // Add sort parameter if provided
        if (!string.IsNullOrWhiteSpace(sort))
        {
            url += $"&sort={HttpUtility.UrlEncode(sort)}";
        }

        _logger.LogDebug("Built search URL: {Url}", url);

        return url;
    }

    /// <summary>
    /// Fetches HTML content with cache support
    /// </summary>
    private async Task<string> FetchHtmlAsync(string url)
    {
        // Check cache first
        var cachedHtml = await _cache.GetAsync<string>(url);
        if (cachedHtml != null)
        {
            _logger.LogInformation("Cache hit for URL: {Url}", url);
            return cachedHtml;
        }

        _logger.LogInformation("Cache miss for URL: {Url}, fetching from Cimri.com", url);

        // Always use Playwright (only method that works)
        _logger.LogInformation("🎭 Fetching {Url} using Playwright", url);
        
        try
        {
            var html = await _playwrightScraper.GetHtmlAsync(url);
            
            if (!string.IsNullOrWhiteSpace(html))
            {
                _logger.LogInformation("✅ Playwright fetched HTML, length: {Length} bytes", html.Length);
                
                // Cache the HTML (even if it's error page, parser will handle it)
                await _cache.SetAsync(url, html);
                
                return html;
            }
            else
            {
                _logger.LogError("❌ Playwright returned empty HTML for {Url}", url);
                return string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Playwright failed for {Url}", url);
            return string.Empty;
        }
    }

    /// <summary>
    /// Parses product list from HTML content
    /// </summary>
    private List<CimriProduct> ParseProductList(string html)
    {
        var products = new List<CimriProduct>();

        try
        {
            // Parse pagination information
            _totalPages = ParsePagination(html);
            _logger.LogDebug("Parsed pagination: {TotalPages} total pages", _totalPages);

            // Try parsing JSON-LD first
            var jsonLdDocs = _htmlParser.ParseJsonLd(html);
            
            if (jsonLdDocs.Any())
            {
                _logger.LogDebug("Parsing products from JSON-LD data");
                products.AddRange(ParseProductsFromJsonLd(jsonLdDocs));
            }

            // Try parsing __NEXT_DATA__
            var nextData = _htmlParser.ParseNextData(html);
            
            if (nextData != null)
            {
                _logger.LogDebug("Parsing products from __NEXT_DATA__");
                var nextDataProducts = ParseProductsFromNextData(nextData);
                
                // Merge or replace products based on what we found
                if (nextDataProducts.Any())
                {
                    products = MergeProductData(products, nextDataProducts);
                }
            }

            _logger.LogInformation("Successfully parsed {Count} products from HTML", products.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing product list from HTML");
        }

        return products;
    }

    /// <summary>
    /// Parses pagination information from HTML
    /// </summary>
    private int ParsePagination(string html)
    {
        return _htmlParser.ParsePagination(html);
    }

    /// <summary>
    /// Parses products from JSON-LD data
    /// </summary>
    private List<CimriProduct> ParseProductsFromJsonLd(List<JsonDocument> jsonLdDocs)
    {
        var products = new List<CimriProduct>();

        foreach (var doc in jsonLdDocs)
        {
            try
            {
                var root = doc.RootElement;

                // Check if this is a Product or ItemList
                if (root.TryGetProperty("@type", out var typeElement))
                {
                    var type = typeElement.GetString();

                    if (type == "Product")
                    {
                        var product = ParseSingleProductFromJsonLd(root);
                        if (product != null)
                        {
                            products.Add(product);
                        }
                    }
                    else if (type == "ItemList")
                    {
                        if (root.TryGetProperty("itemListElement", out var items))
                        {
                            foreach (var item in items.EnumerateArray())
                            {
                                var product = ParseSingleProductFromJsonLd(item);
                                if (product != null)
                                {
                                    products.Add(product);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing JSON-LD document");
            }
        }

        return products;
    }

    /// <summary>
    /// Parses a single product from JSON-LD element
    /// </summary>
    private CimriProduct? ParseSingleProductFromJsonLd(JsonElement element)
    {
        try
        {
            var product = new CimriProduct();

            // Extract name
            if (element.TryGetProperty("name", out var nameElement))
            {
                product.Name = nameElement.GetString() ?? string.Empty;
            }

            // Extract URL
            if (element.TryGetProperty("url", out var urlElement))
            {
                product.ProductUrl = urlElement.GetString() ?? string.Empty;
                
                // Extract ID from URL if possible
                var url = product.ProductUrl;
                if (!string.IsNullOrEmpty(url))
                {
                    var segments = url.Split('/');
                    product.Id = segments.LastOrDefault() ?? string.Empty;
                }
            }

            // Extract image
            if (element.TryGetProperty("image", out var imageElement))
            {
                if (imageElement.ValueKind == JsonValueKind.String)
                {
                    product.ImageUrl = imageElement.GetString() ?? string.Empty;
                }
                else if (imageElement.ValueKind == JsonValueKind.Array)
                {
                    var firstImage = imageElement.EnumerateArray().FirstOrDefault();
                    product.ImageUrl = firstImage.GetString() ?? string.Empty;
                }
            }

            // Extract offers/price
            if (element.TryGetProperty("offers", out var offersElement))
            {
                if (offersElement.TryGetProperty("price", out var priceElement))
                {
                    if (decimal.TryParse(priceElement.GetString(), out var price))
                    {
                        product.Price = price;
                    }
                }

                if (offersElement.TryGetProperty("seller", out var sellerElement))
                {
                    if (sellerElement.TryGetProperty("name", out var sellerNameElement))
                    {
                        product.MerchantName = sellerNameElement.GetString() ?? string.Empty;
                    }
                }
            }

            // Extract brand
            if (element.TryGetProperty("brand", out var brandElement))
            {
                if (brandElement.TryGetProperty("name", out var brandNameElement))
                {
                    product.Brand = brandNameElement.GetString() ?? string.Empty;
                }
                else if (brandElement.ValueKind == JsonValueKind.String)
                {
                    product.Brand = brandElement.GetString() ?? string.Empty;
                }
            }

            // Only return product if it has minimum required fields
            if (!string.IsNullOrEmpty(product.Name) && !string.IsNullOrEmpty(product.ProductUrl))
            {
                return product;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing single product from JSON-LD");
            return null;
        }
    }

    /// <summary>
    /// Parses products from __NEXT_DATA__
    /// </summary>
    private List<CimriProduct> ParseProductsFromNextData(JsonDocument nextData)
    {
        var products = new List<CimriProduct>();

        try
        {
            var root = nextData.RootElement;

            // Navigate to products array (structure may vary)
            // Common paths: props.pageProps.products or props.initialState.products
            if (root.TryGetProperty("props", out var props))
            {
                JsonElement productsArray = default;
                JsonElement searchResult = default;
                bool found = false;

                // Try pageProps.products
                if (props.TryGetProperty("pageProps", out var pageProps))
                {
                    _logger.LogDebug("Found pageProps, checking for products array");
                    
                    // Check if this is an error response
                    if (pageProps.TryGetProperty("statusCode", out var statusCodeElement))
                    {
                        var statusCode = statusCodeElement.GetInt32();
                        if (statusCode != 200)
                        {
                            _logger.LogError("Cimri returned error status code: {StatusCode}. This usually means bot detection. Try clearing cache or using different user agent.", statusCode);
                        }
                    }
                    
                    if (pageProps.TryGetProperty("products", out productsArray))
                    {
                        _logger.LogInformation("Found products array directly in pageProps with {Count} items", productsArray.GetArrayLength());
                        found = true;
                    }
                    else if (pageProps.TryGetProperty("searchResult", out searchResult))
                    {
                        _logger.LogDebug("Checking searchResult for products");
                        if (searchResult.TryGetProperty("products", out productsArray))
                        {
                            _logger.LogInformation("Found products array in searchResult with {Count} items", productsArray.GetArrayLength());
                            found = true;
                        }
                    }
                    // Try data.products (alternative path)
                    else if (pageProps.TryGetProperty("data", out var dataElement))
                    {
                        _logger.LogDebug("Checking data for products");
                        
                        // Try data.products
                        if (dataElement.TryGetProperty("products", out productsArray))
                        {
                            _logger.LogInformation("Found products array in data with {Count} items", productsArray.GetArrayLength());
                            found = true;
                        }
                        // Try data.searchResult.products
                        else if (dataElement.TryGetProperty("searchResult", out searchResult))
                        {
                            if (searchResult.TryGetProperty("products", out productsArray))
                            {
                                _logger.LogInformation("Found products array in data.searchResult with {Count} items", productsArray.GetArrayLength());
                                found = true;
                            }
                        }
                        // Try data.data.products (nested data)
                        else if (dataElement.TryGetProperty("data", out var nestedDataElement))
                        {
                            _logger.LogDebug("Found nested data, checking for products");
                            
                            if (nestedDataElement.TryGetProperty("products", out productsArray))
                            {
                                _logger.LogInformation("Found products array in data.data with {Count} items", productsArray.GetArrayLength());
                                found = true;
                            }
                            else if (nestedDataElement.TryGetProperty("searchResult", out searchResult))
                            {
                                if (searchResult.TryGetProperty("products", out productsArray))
                                {
                                    _logger.LogInformation("Found products array in data.data.searchResult with {Count} items", productsArray.GetArrayLength());
                                    found = true;
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Could not find products in data.data. Available keys: {Keys}", 
                                    string.Join(", ", nestedDataElement.EnumerateObject().Select(p => p.Name)));
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Could not find products in data. Available keys in data: {Keys}", 
                                string.Join(", ", dataElement.EnumerateObject().Select(p => p.Name)));
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Could not find products array in pageProps. Available keys: {Keys}", 
                            string.Join(", ", pageProps.EnumerateObject().Select(p => p.Name)));
                    }

                    // Try to extract pagination info
                    if (pageProps.TryGetProperty("totalPages", out var totalPagesElement))
                    {
                        if (totalPagesElement.TryGetInt32(out var totalPages))
                        {
                            _totalPages = totalPages;
                        }
                    }
                    else if (searchResult.ValueKind != JsonValueKind.Undefined)
                    {
                        if (searchResult.TryGetProperty("totalPages", out totalPagesElement))
                        {
                            if (totalPagesElement.TryGetInt32(out var totalPages))
                            {
                                _totalPages = totalPages;
                            }
                        }
                    }
                }

                if (found && productsArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var productElement in productsArray.EnumerateArray())
                    {
                        var product = ParseSingleProductFromNextData(productElement);
                        if (product != null)
                        {
                            products.Add(product);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing products from __NEXT_DATA__");
        }

        return products;
    }

    /// <summary>
    /// Parses a single product from __NEXT_DATA__ element
    /// </summary>
    private CimriProduct? ParseSingleProductFromNextData(JsonElement element)
    {
        try
        {
            var product = new CimriProduct();

            // Extract ID (can be string or number)
            if (element.TryGetProperty("id", out var idElement))
            {
                if (idElement.ValueKind == JsonValueKind.String)
                {
                    product.Id = idElement.GetString() ?? string.Empty;
                }
                else if (idElement.ValueKind == JsonValueKind.Number)
                {
                    product.Id = idElement.GetInt64().ToString();
                }
            }

            // Extract name (can be "name" or "title")
            if (element.TryGetProperty("title", out var nameElement))
            {
                product.Name = nameElement.GetString() ?? string.Empty;
            }
            else if (element.TryGetProperty("name", out nameElement))
            {
                product.Name = nameElement.GetString() ?? string.Empty;
            }

            // Extract price from offerSummary.minPrice
            if (element.TryGetProperty("offerSummary", out var offerSummaryElement))
            {
                if (offerSummaryElement.TryGetProperty("minPrice", out var minPriceElement))
                {
                    if (minPriceElement.ValueKind == JsonValueKind.Number && minPriceElement.TryGetDecimal(out var minPrice))
                    {
                        product.Price = minPrice;
                    }
                }
            }
            // Fallback to direct price property
            else if (element.TryGetProperty("price", out var priceElement))
            {
                if (priceElement.TryGetDecimal(out var price))
                {
                    product.Price = price;
                }
            }

            // Extract unit price
            if (element.TryGetProperty("unitPrice", out var unitPriceElement))
            {
                if (unitPriceElement.TryGetDecimal(out var unitPrice))
                {
                    product.UnitPrice = unitPrice;
                }
            }

            // Extract image URL from imageIds array
            if (element.TryGetProperty("imageIds", out var imageIdsElement))
            {
                if (imageIdsElement.ValueKind == JsonValueKind.Array && imageIdsElement.GetArrayLength() > 0)
                {
                    var firstImageId = imageIdsElement[0];
                    if (firstImageId.ValueKind == JsonValueKind.Number)
                    {
                        // Use our proxy to avoid CORS issues
                        product.ImageUrl = $"/api/imageproxy/{firstImageId.GetInt64()}";
                    }
                }
            }
            // Fallback to direct image properties
            else if (element.TryGetProperty("imageUrl", out var imageUrlElement))
            {
                product.ImageUrl = imageUrlElement.GetString() ?? string.Empty;
            }
            else if (element.TryGetProperty("image", out var imageElement))
            {
                product.ImageUrl = imageElement.GetString() ?? string.Empty;
            }

            // Extract product URL (can be "url" or "path")
            if (element.TryGetProperty("path", out var urlElement))
            {
                var path = urlElement.GetString() ?? string.Empty;
                product.ProductUrl = path.StartsWith("http") ? path : $"https://www.cimri.com{path}";
            }
            else if (element.TryGetProperty("url", out urlElement))
            {
                product.ProductUrl = urlElement.GetString() ?? string.Empty;
            }

            // Extract merchant info
            if (element.TryGetProperty("merchant", out var merchantElement))
            {
                if (merchantElement.TryGetProperty("id", out var merchantIdElement))
                {
                    product.MerchantId = merchantIdElement.GetString() ?? string.Empty;
                }
                if (merchantElement.TryGetProperty("name", out var merchantNameElement))
                {
                    product.MerchantName = merchantNameElement.GetString() ?? string.Empty;
                }
            }

            // Extract brand from brandSummary
            if (element.TryGetProperty("brandSummary", out var brandSummaryElement))
            {
                if (brandSummaryElement.TryGetProperty("name", out var brandNameElement))
                {
                    product.Brand = brandNameElement.GetString() ?? string.Empty;
                }
            }
            // Fallback to direct brand property
            else if (element.TryGetProperty("brand", out var brandElement))
            {
                product.Brand = brandElement.GetString() ?? string.Empty;
            }

            // Extract quantity and unit
            if (element.TryGetProperty("quantity", out var quantityElement))
            {
                product.Quantity = quantityElement.GetString() ?? string.Empty;
            }
            if (element.TryGetProperty("unit", out var unitElement))
            {
                product.Unit = unitElement.GetString() ?? string.Empty;
            }

            // Only return product if it has minimum required fields
            if (!string.IsNullOrEmpty(product.Id) && 
                !string.IsNullOrEmpty(product.Name) && 
                !string.IsNullOrEmpty(product.ProductUrl))
            {
                return product;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing single product from __NEXT_DATA__");
            return null;
        }
    }

    /// <summary>
    /// Merges product data from different sources
    /// </summary>
    private List<CimriProduct> MergeProductData(List<CimriProduct> jsonLdProducts, List<CimriProduct> nextDataProducts)
    {
        // If we have __NEXT_DATA__ products, prefer those as they're usually more complete
        if (nextDataProducts.Any())
        {
            return nextDataProducts;
        }

        return jsonLdProducts;
    }
}
