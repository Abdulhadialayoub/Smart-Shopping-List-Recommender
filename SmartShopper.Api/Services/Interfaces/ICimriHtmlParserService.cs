using System.Text.Json;

namespace SmartShopper.Api.Services;

/// <summary>
/// Interface for parsing HTML content from Cimri.com and extracting JSON data
/// </summary>
public interface ICimriHtmlParserService
{
    /// <summary>
    /// Parses JSON-LD script tags from HTML content
    /// </summary>
    /// <param name="html">HTML content to parse</param>
    /// <returns>List of parsed JSON-LD objects as JsonDocument, or empty list if none found</returns>
    List<JsonDocument> ParseJsonLd(string html);

    /// <summary>
    /// Parses __NEXT_DATA__ script tag from HTML content (used by Next.js applications)
    /// </summary>
    /// <param name="html">HTML content to parse</param>
    /// <returns>Parsed __NEXT_DATA__ as JsonDocument, or null if not found or parsing fails</returns>
    JsonDocument? ParseNextData(string html);

    /// <summary>
    /// Attempts to extract JSON from a script tag using regex as fallback
    /// </summary>
    /// <param name="html">HTML content</param>
    /// <param name="scriptId">Script tag ID to search for</param>
    /// <returns>Extracted JSON string, or null if not found</returns>
    string? ExtractScriptContentById(string html, string scriptId);

    /// <summary>
    /// Safely attempts to parse JSON string with error handling
    /// </summary>
    /// <param name="jsonString">JSON string to parse</param>
    /// <returns>JsonDocument if successful, null otherwise</returns>
    JsonDocument? TryParseJson(string jsonString);

    /// <summary>
    /// Parses pagination information from HTML content
    /// Supports both mobile format (x/y) and normal format pagination
    /// </summary>
    /// <param name="html">HTML content to parse</param>
    /// <returns>Total number of pages, or 0 if not found</returns>
    int ParsePagination(string html);
}
