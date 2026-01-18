namespace SmartShopper.Api.Services;

/// <summary>
/// Configuration options for Cimri scraper service
/// </summary>
public class CimriScraperOptions
{
    public string BaseUrl { get; set; } = "https://www.cimri.com/market/arama";
    public string CacheDirectory { get; set; } = "./cache/cimri";
    public int CacheDurationMinutes { get; set; } = 60;
    public int MaxCacheFiles { get; set; } = 500;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 2;
    public int MinDelayMs { get; set; } = 1000;
    public int MaxDelayMs { get; set; } = 3000;
    public int RequestTimeoutSeconds { get; set; } = 30;
}
