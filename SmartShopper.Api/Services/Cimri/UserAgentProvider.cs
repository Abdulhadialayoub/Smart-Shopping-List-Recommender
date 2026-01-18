namespace SmartShopper.Api.Services;

public class UserAgentProvider : IUserAgentProvider
{
    private static readonly string[] UserAgents = new[]
    {
        // Chrome on Windows
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
        
        // Chrome on Mac
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
        
        // Chrome on Linux
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
        
        // Firefox on Windows
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:120.0) Gecko/20100101 Firefox/120.0",
        
        // Firefox on Mac
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:121.0) Gecko/20100101 Firefox/121.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:120.0) Gecko/20100101 Firefox/120.0",
        
        // Firefox on Linux
        "Mozilla/5.0 (X11; Linux x86_64; rv:121.0) Gecko/20100101 Firefox/121.0",
        "Mozilla/5.0 (X11; Linux x86_64; rv:120.0) Gecko/20100101 Firefox/120.0",
        
        // Safari on Mac
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Safari/605.1.15",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15",
        
        // Safari on iOS
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Mobile/15E148 Safari/604.1",
        "Mozilla/5.0 (iPad; CPU OS 17_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Mobile/15E148 Safari/604.1",
        
        // Edge on Windows
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36 Edg/119.0.0.0"
    };

    private readonly Random _random = new Random();

    public string GetRandomUserAgent()
    {
        int index = _random.Next(UserAgents.Length);
        return UserAgents[index];
    }

    public string GetSecChUaHeader(string userAgent)
    {
        // Extract browser and version from user agent
        if (userAgent.Contains("Chrome") && userAgent.Contains("Edg"))
        {
            // Edge
            var version = ExtractVersion(userAgent, "Edg/");
            return $"\"Microsoft Edge\";v=\"{version}\", \"Chromium\";v=\"{version}\", \"Not=A?Brand\";v=\"99\"";
        }
        else if (userAgent.Contains("Chrome") && !userAgent.Contains("Edg"))
        {
            // Chrome
            var version = ExtractVersion(userAgent, "Chrome/");
            return $"\"Google Chrome\";v=\"{version}\", \"Chromium\";v=\"{version}\", \"Not=A?Brand\";v=\"99\"";
        }
        else if (userAgent.Contains("Firefox"))
        {
            // Firefox doesn't use sec-ch-ua header
            return string.Empty;
        }
        else if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome"))
        {
            // Safari doesn't use sec-ch-ua header
            return string.Empty;
        }

        return string.Empty;
    }

    private string ExtractVersion(string userAgent, string prefix)
    {
        var startIndex = userAgent.IndexOf(prefix);
        if (startIndex == -1) return "120";

        startIndex += prefix.Length;
        var endIndex = userAgent.IndexOf('.', startIndex);
        if (endIndex == -1) return "120";

        return userAgent.Substring(startIndex, endIndex - startIndex);
    }
}
