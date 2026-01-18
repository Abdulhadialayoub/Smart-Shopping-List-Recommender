using AngleSharp;
using AngleSharp.Html.Parser;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SmartShopper.Api.Services;

/// <summary>
/// Service for parsing HTML content from Cimri.com and extracting JSON data
/// </summary>
public class CimriHtmlParserService : ICimriHtmlParserService
{
    private readonly ILogger<CimriHtmlParserService> _logger;
    private readonly HtmlParser _htmlParser;

    public CimriHtmlParserService(ILogger<CimriHtmlParserService> logger)
    {
        _logger = logger;
        _htmlParser = new HtmlParser();
    }

    /// <summary>
    /// Parses JSON-LD script tags from HTML content
    /// </summary>
    /// <param name="html">HTML content to parse</param>
    /// <returns>List of parsed JSON-LD objects as JsonDocument, or empty list if none found</returns>
    public List<JsonDocument> ParseJsonLd(string html)
    {
        var results = new List<JsonDocument>();

        try
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                _logger.LogWarning("ParseJsonLd: HTML content is null or empty");
                return results;
            }

            var document = _htmlParser.ParseDocument(html);
            var jsonLdScripts = document.QuerySelectorAll("script[type='application/ld+json']");

            _logger.LogDebug("Found {Count} JSON-LD script tags", jsonLdScripts.Length);

            foreach (var script in jsonLdScripts)
            {
                var jsonContent = script.TextContent?.Trim();
                
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    _logger.LogDebug("Skipping empty JSON-LD script tag");
                    continue;
                }

                try
                {
                    var jsonDoc = JsonDocument.Parse(jsonContent);
                    results.Add(jsonDoc);
                    _logger.LogDebug("Successfully parsed JSON-LD: {Preview}", 
                        jsonContent.Length > 100 ? jsonContent.Substring(0, 100) + "..." : jsonContent);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse JSON-LD content. Content preview: {Preview}", 
                        jsonContent.Length > 200 ? jsonContent.Substring(0, 200) + "..." : jsonContent);
                    // Continue processing other JSON-LD tags even if one fails
                }
            }

            _logger.LogInformation("Successfully parsed {Count} JSON-LD objects from HTML", results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing HTML for JSON-LD. HTML preview: {Preview}", 
                html.Length > 500 ? html.Substring(0, 500) + "..." : html);
            // Return empty list instead of throwing
        }

        return results;
    }

    /// <summary>
    /// Parses __NEXT_DATA__ script tag from HTML content (used by Next.js applications)
    /// </summary>
    /// <param name="html">HTML content to parse</param>
    /// <returns>Parsed __NEXT_DATA__ as JsonDocument, or null if not found or parsing fails</returns>
    public JsonDocument? ParseNextData(string html)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                _logger.LogWarning("ParseNextData: HTML content is null or empty");
                return null;
            }

            var document = _htmlParser.ParseDocument(html);
            
            // Look for script tag with id="__NEXT_DATA__"
            var nextDataScript = document.QuerySelector("script#__NEXT_DATA__");

            if (nextDataScript == null)
            {
                _logger.LogDebug("__NEXT_DATA__ script tag not found in HTML");
                return null;
            }

            var jsonContent = nextDataScript.TextContent?.Trim();

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _logger.LogWarning("__NEXT_DATA__ script tag found but content is empty");
                return null;
            }

            try
            {
                var jsonDoc = JsonDocument.Parse(jsonContent);
                _logger.LogInformation("Successfully parsed __NEXT_DATA__. Content size: {Size} bytes", 
                    jsonContent.Length);
                return jsonDoc;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse __NEXT_DATA__ JSON content. Content preview: {Preview}", 
                    jsonContent.Length > 200 ? jsonContent.Substring(0, 200) + "..." : jsonContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing HTML for __NEXT_DATA__. HTML preview: {Preview}", 
                html.Length > 500 ? html.Substring(0, 500) + "..." : html);
            return null;
        }
    }

    /// <summary>
    /// Attempts to extract JSON from a script tag using regex as fallback
    /// This is useful when the HTML parser fails or for malformed HTML
    /// </summary>
    /// <param name="html">HTML content</param>
    /// <param name="scriptId">Script tag ID to search for</param>
    /// <returns>Extracted JSON string, or null if not found</returns>
    public string? ExtractScriptContentById(string html, string scriptId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(html) || string.IsNullOrWhiteSpace(scriptId))
            {
                return null;
            }

            // Pattern to match: <script id="scriptId" ...>content</script>
            var pattern = $@"<script[^>]*id\s*=\s*[""']{Regex.Escape(scriptId)}[""'][^>]*>(.*?)</script>";
            var match = Regex.Match(html, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (match.Success && match.Groups.Count > 1)
            {
                var content = match.Groups[1].Value.Trim();
                _logger.LogDebug("Extracted script content for id '{ScriptId}', size: {Size} bytes", 
                    scriptId, content.Length);
                return content;
            }

            _logger.LogDebug("Script tag with id '{ScriptId}' not found using regex", scriptId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting script content for id '{ScriptId}'", scriptId);
            return null;
        }
    }

    /// <summary>
    /// Safely attempts to parse JSON string with error handling
    /// </summary>
    /// <param name="jsonString">JSON string to parse</param>
    /// <returns>JsonDocument if successful, null otherwise</returns>
    public JsonDocument? TryParseJson(string jsonString)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return null;
            }

            return JsonDocument.Parse(jsonString);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON string. Preview: {Preview}", 
                jsonString.Length > 200 ? jsonString.Substring(0, 200) + "..." : jsonString);
            return null;
        }
    }

    /// <summary>
    /// Parses pagination information from HTML content
    /// Supports both mobile format (x/y) and normal format pagination
    /// </summary>
    /// <param name="html">HTML content to parse</param>
    /// <returns>Total number of pages, or 0 if not found</returns>
    public int ParsePagination(string html)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                _logger.LogWarning("ParsePagination: HTML content is null or empty");
                return 0;
            }

            var document = _htmlParser.ParseDocument(html);

            // Try to find pagination in various common locations
            
            // 1. Look for mobile format pagination (e.g., "1/10" or "Sayfa 1/10")
            var paginationTexts = document.QuerySelectorAll("div[class*='pagination'], span[class*='page'], div[class*='page']");
            foreach (var element in paginationTexts)
            {
                var text = element.TextContent?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                // Match patterns like "1/10", "Sayfa 1/10", "Page 1 of 10"
                var mobileMatch = Regex.Match(text, @"(\d+)\s*/\s*(\d+)");
                if (mobileMatch.Success && mobileMatch.Groups.Count > 2)
                {
                    if (int.TryParse(mobileMatch.Groups[2].Value, out var totalPages))
                    {
                        _logger.LogDebug("Found pagination in mobile format: {Text}, total pages: {TotalPages}", 
                            text, totalPages);
                        return totalPages;
                    }
                }

                // Match patterns like "Page 1 of 10"
                var ofMatch = Regex.Match(text, @"(\d+)\s+of\s+(\d+)", RegexOptions.IgnoreCase);
                if (ofMatch.Success && ofMatch.Groups.Count > 2)
                {
                    if (int.TryParse(ofMatch.Groups[2].Value, out var totalPages))
                    {
                        _logger.LogDebug("Found pagination in 'of' format: {Text}, total pages: {TotalPages}", 
                            text, totalPages);
                        return totalPages;
                    }
                }
            }

            // 2. Look for pagination links (last page number)
            var pageLinks = document.QuerySelectorAll("a[class*='pagination'], a[class*='page'], button[class*='page']");
            int maxPage = 0;
            foreach (var link in pageLinks)
            {
                var text = link.TextContent?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                // Try to parse as number
                if (int.TryParse(text, out var pageNum))
                {
                    if (pageNum > maxPage)
                    {
                        maxPage = pageNum;
                    }
                }

                // Also check href attribute for page numbers
                var href = link.GetAttribute("href");
                if (!string.IsNullOrWhiteSpace(href))
                {
                    var pageMatch = Regex.Match(href, @"[?&]page=(\d+)");
                    if (pageMatch.Success && pageMatch.Groups.Count > 1)
                    {
                        if (int.TryParse(pageMatch.Groups[1].Value, out var pageNum2))
                        {
                            if (pageNum2 > maxPage)
                            {
                                maxPage = pageNum2;
                            }
                        }
                    }
                }
            }

            if (maxPage > 0)
            {
                _logger.LogDebug("Found pagination from page links, max page: {MaxPage}", maxPage);
                return maxPage;
            }

            // 3. Try to find in __NEXT_DATA__ or other JSON data
            var nextData = ParseNextData(html);
            if (nextData != null)
            {
                var root = nextData.RootElement;
                if (root.TryGetProperty("props", out var props))
                {
                    if (props.TryGetProperty("pageProps", out var pageProps))
                    {
                        if (pageProps.TryGetProperty("totalPages", out var totalPagesElement))
                        {
                            if (totalPagesElement.TryGetInt32(out var totalPages))
                            {
                                _logger.LogDebug("Found pagination in __NEXT_DATA__, total pages: {TotalPages}", 
                                    totalPages);
                                return totalPages;
                            }
                        }

                        if (pageProps.TryGetProperty("searchResult", out var searchResult))
                        {
                            if (searchResult.TryGetProperty("totalPages", out totalPagesElement))
                            {
                                if (totalPagesElement.TryGetInt32(out var totalPages))
                                {
                                    _logger.LogDebug("Found pagination in __NEXT_DATA__ searchResult, total pages: {TotalPages}", 
                                        totalPages);
                                    return totalPages;
                                }
                            }
                        }
                    }
                }
            }

            _logger.LogDebug("No pagination information found in HTML");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing pagination from HTML. HTML preview: {Preview}", 
                html.Length > 500 ? html.Substring(0, 500) + "..." : html);
            return 0;
        }
    }
}
