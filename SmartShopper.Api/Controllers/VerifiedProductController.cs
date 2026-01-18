using Microsoft.AspNetCore.Mvc;
using SmartShopper.Api.Services.DualModel;
using System.ComponentModel.DataAnnotations;

namespace SmartShopper.Api.Controllers;

/// <summary>
/// Controller for dual-model verified product recommendations.
/// Uses Groq for fast generation and Gemini for validation.
/// </summary>
[ApiController]
[Route("api/ai")]
[Produces("application/json")]
public class VerifiedProductController : ControllerBase
{
    private readonly IDualModelVerificationService _verificationService;
    private readonly IInputValidationService _validationService;
    private readonly IOutputSanitizationService _sanitizationService;
    private readonly ILogger<VerifiedProductController> _logger;

    public VerifiedProductController(
        IDualModelVerificationService verificationService,
        IInputValidationService validationService,
        IOutputSanitizationService sanitizationService,
        ILogger<VerifiedProductController> logger)
    {
        _verificationService = verificationService;
        _validationService = validationService;
        _sanitizationService = sanitizationService;
        _logger = logger;
    }

    /// <summary>
    /// Generate verified product recommendations using dual-model AI pipeline.
    /// </summary>
    /// <param name="request">Product recommendation request with shopping list</param>
    /// <param name="skipValidation">Skip Gemini validation stage (for testing)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verified product recommendations with metadata</returns>
    /// <response code="200">Returns the verified product recommendations</response>
    /// <response code="400">If request validation fails</response>
    /// <response code="429">If rate limit is exceeded</response>
    /// <response code="500">If an error occurs during generation</response>
    [HttpPost("verified-products")]
    [ProducesResponseType(typeof(VerifiedProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateVerifiedProducts(
        [FromBody] VerifiedProductRequest request,
        [FromQuery] bool skipValidation = false,
        CancellationToken cancellationToken = default)
    {
        // Input validation
        if (request == null || request.ShoppingList == null || !request.ShoppingList.Any())
        {
            _logger.LogWarning("Verified product request received with empty shopping list");
            return BadRequest(new ErrorResponse
            {
                Error = "Invalid request",
                Message = "Shopping list is required and must contain at least one item"
            });
        }

        // Validate and sanitize shopping list
        var validationResult = _validationService.ValidateShoppingList(request.ShoppingList);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Verified product request validation failed: {Errors}", 
                string.Join(", ", validationResult.Errors));
            return BadRequest(new ErrorResponse
            {
                Error = "Invalid request",
                Message = string.Join("; ", validationResult.Errors)
            });
        }

        // Use sanitized shopping list
        var sanitizedShoppingList = validationResult.Value!;

        try
        {
            _logger.LogInformation("Generating verified product recommendations for {Count} items, skipValidation: {Skip}",
                sanitizedShoppingList.Count, skipValidation);

            var result = await _verificationService.GenerateVerifiedProductRecommendationsAsync(
                sanitizedShoppingList,
                cancellationToken);

            // Sanitize output
            result = _sanitizationService.SanitizeProductResponse(result);

            _logger.LogInformation("Verified product recommendations generated successfully: {Count} recommendations, validated: {Validated}",
                result.Recommendations.Count, result.Metadata.WasValidated);

            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Product recommendation generation cancelled");
            return StatusCode(StatusCodes.Status408RequestTimeout, new ErrorResponse
            {
                Error = "Request timeout",
                Message = "Product recommendation generation took too long and was cancelled"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating verified product recommendations");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Error = "Product recommendation generation failed",
                Message = "An error occurred while generating product recommendations. Please try again later."
            });
        }
    }
}

/// <summary>
/// Request model for verified product recommendations.
/// </summary>
public class VerifiedProductRequest
{
    /// <summary>
    /// List of items user wants to purchase.
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required")]
    [MaxLength(50, ErrorMessage = "Maximum 50 items allowed")]
    public List<string> ShoppingList { get; set; } = new();
}
