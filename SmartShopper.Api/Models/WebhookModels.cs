namespace SmartShopper.Api.Models;

// n8n entegrasyonu için model sınıfları

/// <summary>
/// Buzdolabı özet bilgileri - n8n workflow'ları için
/// </summary>
public class FridgeSummary
{
    public string UserId { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int ExpiringItems { get; set; }
    public int ExpiredItems { get; set; }
    public Dictionary<string, int> Categories { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Akıllı alışveriş listesi oluşturma isteği - n8n için
/// </summary>
public class SmartShoppingListRequest
{
    public string UserId { get; set; } = string.Empty;
    public bool IncludeRecipeSuggestions { get; set; } = true;
    public int MaxRecipes { get; set; } = 3;
}

/// <summary>
/// Akıllı alışveriş listesi yanıtı - n8n için
/// </summary>
public class SmartShoppingListResponse
{
    public ShoppingList ShoppingList { get; set; } = new();
    public List<Recipe> SuggestedRecipes { get; set; } = new();
    public double TotalEstimatedCost { get; set; }
}

/// <summary>
/// Toplu işlem isteği - n8n için birden fazla operasyon
/// </summary>
public class BatchOperationRequest
{
    public List<BatchOperation> Operations { get; set; } = new();
}

/// <summary>
/// Tek bir toplu işlem
/// </summary>
public class BatchOperation
{
    public string Type { get; set; } = string.Empty;
    public object Data { get; set; } = new();
}

/// <summary>
/// Toplu işlem yanıtı
/// </summary>
public class BatchOperationResponse
{
    public List<object> Results { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
}
