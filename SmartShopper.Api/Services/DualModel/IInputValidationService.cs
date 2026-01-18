namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Service for validating and sanitizing user inputs for the dual-model verification system
/// </summary>
public interface IInputValidationService
{
    /// <summary>
    /// Validates and sanitizes a user inventory list
    /// </summary>
    /// <param name="inventory">The user inventory to validate</param>
    /// <returns>Validation result with sanitized inventory</returns>
    ValidationResult<List<string>> ValidateUserInventory(List<string>? inventory);

    /// <summary>
    /// Validates and sanitizes a shopping list
    /// </summary>
    /// <param name="shoppingList">The shopping list to validate</param>
    /// <returns>Validation result with sanitized shopping list</returns>
    ValidationResult<List<string>> ValidateShoppingList(List<string>? shoppingList);

    /// <summary>
    /// Sanitizes a single ingredient name by removing special characters
    /// </summary>
    /// <param name="ingredientName">The ingredient name to sanitize</param>
    /// <returns>Sanitized ingredient name</returns>
    string SanitizeIngredientName(string ingredientName);
}

/// <summary>
/// Result of input validation
/// </summary>
public class ValidationResult<T>
{
    public bool IsValid { get; set; }
    public T? Value { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ValidationResult<T> Success(T value) => new()
    {
        IsValid = true,
        Value = value,
        Errors = new()
    };

    public static ValidationResult<T> Failure(params string[] errors) => new()
    {
        IsValid = false,
        Value = default,
        Errors = errors.ToList()
    };
}
