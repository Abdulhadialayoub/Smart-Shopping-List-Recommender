using System.Text.RegularExpressions;

namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Service for validating and sanitizing user inputs for the dual-model verification system
/// </summary>
public class InputValidationService : IInputValidationService
{
    private const int MaxInventorySize = 100;
    private const int MaxShoppingListSize = 50;
    private const int MaxIngredientNameLength = 100;
    
    // Regex to match allowed characters: letters, numbers, spaces, hyphens, and common food characters
    private static readonly Regex AllowedCharactersRegex = new(@"[^a-zA-Z0-9\s\-.,()/'çğıöşüÇĞİÖŞÜ]", RegexOptions.Compiled);

    /// <summary>
    /// Validates and sanitizes a user inventory list
    /// Requirements: 2.3 - Consider User_Inventory to avoid duplicate suggestions
    /// Requirements: 2.4 - Prioritize commonly needed ingredients
    /// </summary>
    public ValidationResult<List<string>> ValidateUserInventory(List<string>? inventory)
    {
        if (inventory == null || inventory.Count == 0)
        {
            return ValidationResult<List<string>>.Failure("User inventory cannot be empty");
        }

        if (inventory.Count > MaxInventorySize)
        {
            return ValidationResult<List<string>>.Failure(
                $"User inventory exceeds maximum size of {MaxInventorySize} items. Current size: {inventory.Count}");
        }

        var sanitizedInventory = new List<string>();
        var errors = new List<string>();

        for (int i = 0; i < inventory.Count; i++)
        {
            var item = inventory[i];
            
            if (string.IsNullOrWhiteSpace(item))
            {
                errors.Add($"Item at index {i} is empty or whitespace");
                continue;
            }

            var sanitized = SanitizeIngredientName(item);
            
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                errors.Add($"Item at index {i} contains only invalid characters: '{item}'");
                continue;
            }

            if (sanitized.Length > MaxIngredientNameLength)
            {
                errors.Add($"Item at index {i} exceeds maximum length of {MaxIngredientNameLength} characters");
                continue;
            }

            sanitizedInventory.Add(sanitized);
        }

        if (sanitizedInventory.Count == 0)
        {
            return ValidationResult<List<string>>.Failure("No valid items in user inventory after sanitization");
        }

        // Remove duplicates (case-insensitive)
        sanitizedInventory = sanitizedInventory
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (errors.Count > 0)
        {
            return new ValidationResult<List<string>>
            {
                IsValid = false,
                Value = sanitizedInventory,
                Errors = errors
            };
        }

        return ValidationResult<List<string>>.Success(sanitizedInventory);
    }

    /// <summary>
    /// Validates and sanitizes a shopping list
    /// Requirements: 2.3 - Consider User_Inventory to avoid duplicate suggestions
    /// Requirements: 2.4 - Prioritize commonly needed ingredients
    /// </summary>
    public ValidationResult<List<string>> ValidateShoppingList(List<string>? shoppingList)
    {
        if (shoppingList == null || shoppingList.Count == 0)
        {
            return ValidationResult<List<string>>.Failure("Shopping list cannot be empty");
        }

        if (shoppingList.Count > MaxShoppingListSize)
        {
            return ValidationResult<List<string>>.Failure(
                $"Shopping list exceeds maximum size of {MaxShoppingListSize} items. Current size: {shoppingList.Count}");
        }

        var sanitizedList = new List<string>();
        var errors = new List<string>();

        for (int i = 0; i < shoppingList.Count; i++)
        {
            var item = shoppingList[i];
            
            if (string.IsNullOrWhiteSpace(item))
            {
                errors.Add($"Item at index {i} is empty or whitespace");
                continue;
            }

            var sanitized = SanitizeIngredientName(item);
            
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                errors.Add($"Item at index {i} contains only invalid characters: '{item}'");
                continue;
            }

            if (sanitized.Length > MaxIngredientNameLength)
            {
                errors.Add($"Item at index {i} exceeds maximum length of {MaxIngredientNameLength} characters");
                continue;
            }

            sanitizedList.Add(sanitized);
        }

        if (sanitizedList.Count == 0)
        {
            return ValidationResult<List<string>>.Failure("No valid items in shopping list after sanitization");
        }

        // Remove duplicates (case-insensitive)
        sanitizedList = sanitizedList
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (errors.Count > 0)
        {
            return new ValidationResult<List<string>>
            {
                IsValid = false,
                Value = sanitizedList,
                Errors = errors
            };
        }

        return ValidationResult<List<string>>.Success(sanitizedList);
    }

    /// <summary>
    /// Sanitizes a single ingredient name by removing special characters
    /// Allows: letters, numbers, spaces, hyphens, periods, commas, parentheses, apostrophes, slashes
    /// Also allows Turkish characters: çğıöşüÇĞİÖŞÜ
    /// </summary>
    public string SanitizeIngredientName(string ingredientName)
    {
        if (string.IsNullOrWhiteSpace(ingredientName))
        {
            return string.Empty;
        }

        // Remove special characters but keep allowed ones
        var sanitized = AllowedCharactersRegex.Replace(ingredientName, "");
        
        // Normalize whitespace (replace multiple spaces with single space)
        sanitized = Regex.Replace(sanitized, @"\s+", " ");
        
        // Trim leading/trailing whitespace
        sanitized = sanitized.Trim();

        return sanitized;
    }
}
