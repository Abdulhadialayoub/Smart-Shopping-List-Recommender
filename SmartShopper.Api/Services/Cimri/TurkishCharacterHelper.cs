using System.Text;

namespace SmartShopper.Api.Services;

/// <summary>
/// Helper class for Turkish character normalization and URL-safe conversion
/// </summary>
public static class TurkishCharacterHelper
{
    /// <summary>
    /// Converts Turkish characters to URL-safe equivalents
    /// ş->s, ı->i, ğ->g, ü->u, ö->o, ç->c (and uppercase variants)
    /// </summary>
    /// <param name="input">Input string with Turkish characters</param>
    /// <returns>URL-safe string with Turkish characters normalized</returns>
    public static string ConvertToUrlSafe(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var sb = new StringBuilder(input.Length);

        foreach (char c in input)
        {
            var normalized = c switch
            {
                'ş' => 's',
                'Ş' => 'S',
                'ı' => 'i',
                'İ' => 'I',
                'ğ' => 'g',
                'Ğ' => 'G',
                'ü' => 'u',
                'Ü' => 'U',
                'ö' => 'o',
                'Ö' => 'O',
                'ç' => 'c',
                'Ç' => 'C',
                _ => c
            };

            sb.Append(normalized);
        }

        return sb.ToString();
    }
}
