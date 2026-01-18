using Microsoft.Playwright;

namespace SmartShopper.Api.Services;

public class ScraperService
{
    private readonly ILogger<ScraperService> _logger;

    public ScraperService(ILogger<ScraperService> logger)
    {
        _logger = logger;
    }

    public async Task<string> GetHtmlAsync(string url)
    {
        _logger.LogInformation("Fetching {Url} with Playwright", url);
        
        using var playwright = await Playwright.CreateAsync();
        
        // Launch browser with stealth options
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = new[]
            {
                "--disable-blink-features=AutomationControlled",
                "--disable-dev-shm-usage",
                "--no-sandbox",
                "--disable-setuid-sandbox"
            }
        });

        // Create context with realistic browser fingerprint
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            Locale = "tr-TR",
            TimezoneId = "Europe/Istanbul",
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8" },
                { "Accept-Language", "tr-TR,tr;q=0.9" },
                { "Accept-Encoding", "gzip, deflate, br" }
            }
        });

        var page = await context.NewPageAsync();

        // Inject stealth scripts
        await page.AddInitScriptAsync(@"
            Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
            Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });
            Object.defineProperty(navigator, 'languages', { get: () => ['tr-TR', 'tr'] });
            window.chrome = { runtime: {} };
        ");

        try
        {
            // Simple navigation with load wait
            await page.GotoAsync(url, new PageGotoOptions 
            { 
                WaitUntil = WaitUntilState.Load,
                Timeout = 45000
            });

            // Wait a bit for JS to execute
            await page.WaitForTimeoutAsync(3000);

            var html = await page.ContentAsync();
            _logger.LogInformation("Successfully fetched HTML, length: {Length} bytes", html.Length);
            
            return html;
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout fetching {Url}. Cimri may be blocking the request.", url);
            throw new Exception($"Cimri is blocking the request. The page took too long to load. This usually means aggressive bot detection.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching {Url}", url);
            throw;
        }
    }
}
