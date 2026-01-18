using System.Text;
using System.Text.RegularExpressions;

namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Service for detecting and repairing common JSON errors in AI-generated responses
/// </summary>
public class JsonRepairService
{
    /// <summary>
    /// Attempts to repair common JSON errors
    /// </summary>
    /// <param name="json">The potentially malformed JSON string</param>
    /// <returns>Repaired JSON string</returns>
    public string RepairJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        var repaired = json.Trim();

        // Step 1: Remove trailing commas before closing brackets/braces
        repaired = RemoveTrailingCommas(repaired);

        // Step 2: Fix unclosed brackets and braces
        repaired = FixUnclosedBrackets(repaired);

        // Step 3: Fix missing quotes around property names
        repaired = FixMissingPropertyQuotes(repaired);

        // Step 4: Fix single quotes to double quotes
        repaired = FixSingleQuotes(repaired);

        // Step 5: Remove control characters that break JSON
        repaired = RemoveControlCharacters(repaired);

        return repaired;
    }

    /// <summary>
    /// Removes trailing commas before closing brackets or braces
    /// </summary>
    private string RemoveTrailingCommas(string json)
    {
        // Remove comma before closing brace
        json = Regex.Replace(json, @",\s*}", "}");
        
        // Remove comma before closing bracket
        json = Regex.Replace(json, @",\s*]", "]");

        return json;
    }

    /// <summary>
    /// Fixes unclosed brackets and braces by counting and balancing them
    /// </summary>
    private string FixUnclosedBrackets(string json)
    {
        var result = new StringBuilder(json);
        
        // Count opening and closing brackets/braces
        int openBraces = 0;
        int closeBraces = 0;
        int openBrackets = 0;
        int closeBrackets = 0;
        bool inString = false;
        char prevChar = '\0';

        foreach (char c in json)
        {
            // Track if we're inside a string (ignore brackets/braces in strings)
            if (c == '"' && prevChar != '\\')
            {
                inString = !inString;
            }

            if (!inString)
            {
                if (c == '{') openBraces++;
                else if (c == '}') closeBraces++;
                else if (c == '[') openBrackets++;
                else if (c == ']') closeBrackets++;
            }

            prevChar = c;
        }

        // Add missing closing braces
        for (int i = 0; i < openBraces - closeBraces; i++)
        {
            result.Append('}');
        }

        // Add missing closing brackets
        for (int i = 0; i < openBrackets - closeBrackets; i++)
        {
            result.Append(']');
        }

        return result.ToString();
    }

    /// <summary>
    /// Fixes missing quotes around JSON property names
    /// </summary>
    private string FixMissingPropertyQuotes(string json)
    {
        // This is a simplified approach - only fix obvious unquoted property names
        // Pattern: word characters followed by colon, not already quoted
        var result = new StringBuilder();
        var i = 0;
        
        while (i < json.Length)
        {
            // Check if we're at a potential property name
            if ((i == 0 || json[i - 1] == '{' || json[i - 1] == ',' || char.IsWhiteSpace(json[i - 1])) &&
                char.IsLetter(json[i]))
            {
                // Look ahead to find colon
                var j = i;
                while (j < json.Length && (char.IsLetterOrDigit(json[j]) || json[j] == '_'))
                {
                    j++;
                }
                
                // Skip whitespace
                while (j < json.Length && char.IsWhiteSpace(json[j]))
                {
                    j++;
                }
                
                // If we found a colon, this is likely an unquoted property name
                if (j < json.Length && json[j] == ':')
                {
                    // Check if it's not already quoted
                    var beforeProperty = i > 0 ? json[i - 1] : ' ';
                    if (beforeProperty != '"')
                    {
                        // Add opening quote
                        result.Append('"');
                        // Add property name
                        result.Append(json.Substring(i, j - i).Trim());
                        // Add closing quote
                        result.Append('"');
                        i = j;
                        continue;
                    }
                }
            }
            
            result.Append(json[i]);
            i++;
        }

        return result.ToString();
    }

    /// <summary>
    /// Converts single quotes to double quotes (JSON standard)
    /// </summary>
    private string FixSingleQuotes(string json)
    {
        var result = new StringBuilder();
        bool inDoubleQuote = false;
        char prevChar = '\0';

        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];

            // Track if we're in a double-quoted string
            if (c == '"' && prevChar != '\\')
            {
                inDoubleQuote = !inDoubleQuote;
                result.Append(c);
            }
            // Convert single quotes to double quotes if not inside a double-quoted string
            else if (c == '\'' && !inDoubleQuote)
            {
                result.Append('"');
            }
            else
            {
                result.Append(c);
            }

            prevChar = c;
        }

        return result.ToString();
    }

    /// <summary>
    /// Removes control characters that can break JSON parsing
    /// </summary>
    private string RemoveControlCharacters(string json)
    {
        // Remove control characters except for valid whitespace (space, tab, newline, carriage return)
        return Regex.Replace(json, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");
    }

    /// <summary>
    /// Validates if a string is valid JSON
    /// </summary>
    /// <param name="json">The JSON string to validate</param>
    /// <returns>True if valid JSON, false otherwise</returns>
    public bool IsValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
