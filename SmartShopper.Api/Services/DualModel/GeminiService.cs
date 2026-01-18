using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Implementation of IGeminiService for validation and correction using Gemini 2.0 Flash.
/// </summary>
public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiService> _logger;
    private readonly GeminiOptions _options;
    private readonly int _timeoutSeconds;

    public GeminiService(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        IOptions<DualModelVerificationOptions> verificationOptions,
        ILogger<GeminiService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        _timeoutSeconds = verificationOptions?.Value?.ValidatorTimeoutSeconds ?? 15;

        // Configure HttpClient
        var baseUrl = _options.BaseUrl.TrimEnd('/') + "/";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);
    }

    public async Task<string> ValidateAndCorrectRecipeAsync(
        string draftRecipeJson,
        List<string> userInventory,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(draftRecipeJson))
        {
            throw new ArgumentException("Draft recipe JSON cannot be null or empty", nameof(draftRecipeJson));
        }

        if (userInventory == null || !userInventory.Any())
        {
            throw new ArgumentException("User inventory cannot be null or empty", nameof(userInventory));
        }

        _logger.LogInformation("Validating recipe with Gemini for {Count} inventory items", userInventory.Count);

        var prompt = BuildRecipeValidationPrompt(draftRecipeJson, userInventory);
        var response = await CallGeminiApiAsync(prompt, cancellationToken);

        _logger.LogInformation("Successfully validated recipe");
        return response;
    }

    public async Task<string> ValidateAndCorrectProductRecommendationsAsync(
        string draftRecommendationsJson,
        List<string> shoppingList,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(draftRecommendationsJson))
        {
            throw new ArgumentException("Draft recommendations JSON cannot be null or empty", nameof(draftRecommendationsJson));
        }

        if (shoppingList == null || !shoppingList.Any())
        {
            throw new ArgumentException("Shopping list cannot be null or empty", nameof(shoppingList));
        }

        _logger.LogInformation("Validating product recommendations with Gemini for {Count} items", shoppingList.Count);

        var prompt = BuildProductValidationPrompt(draftRecommendationsJson, shoppingList);
        var response = await CallGeminiApiAsync(prompt, cancellationToken);

        _logger.LogInformation("Successfully validated product recommendations");
        return response;
    }

    private string BuildRecipeValidationPrompt(string draftRecipeJson, List<string> userInventory)
    {
        var inventoryList = string.Join(", ", userInventory);

        return $@"You are a food engineer performing quality control on a recipe.

USER INVENTORY: {inventoryList}
GENERATED RECIPE: {draftRecipeJson}

VALIDATION CHECKLIST:
1. Are all recipe ingredients either in USER INVENTORY or listed in missingIngredients?
2. Are portion sizes realistic? (e.g., not ""5kg sugar"" for 4 servings)
3. Are cooking steps logically ordered?
4. Are cooking times and temperatures appropriate?
5. Is the missingIngredients list complete and accurate?

If errors exist, correct them. Return ONLY the corrected JSON in the same format.
If no errors, return the original JSON unchanged.
Return ONLY valid JSON, no additional text or explanation.";
    }

    private string BuildProductValidationPrompt(string draftRecommendationsJson, List<string> shoppingList)
    {
        var itemsList = string.Join(", ", shoppingList);

        return $@"You are a quality control specialist for product recommendations.

SHOPPING LIST: {itemsList}
GENERATED RECOMMENDATIONS: {draftRecommendationsJson}

VALIDATION CHECKLIST:
1. Are product names specific and searchable?
2. Are quantities realistic?
3. Are there any hallucinated or nonsensical products?
4. Does each recommendation have clear reasoning?

If errors exist, correct them. Return ONLY the corrected JSON in the same format.
If no errors, return the original JSON unchanged.
Return ONLY valid JSON, no additional text or explanation.";
    }

    private async Task<string> CallGeminiApiAsync(string prompt, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = _options.Temperature,
                maxOutputTokens = _options.MaxTokens,
                topP = 0.95,
                topK = 40
            }
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var url = $"models/{_options.Model}:generateContent?key={_options.ApiKey}";
            var response = await _httpClient.PostAsync(url, content, linkedCts.Token);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(linkedCts.Token);
            var responseJson = JsonDocument.Parse(responseContent);

            var generatedText = responseJson.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(generatedText))
            {
                throw new InvalidOperationException("Gemini API returned empty response");
            }

            return generatedText.Trim();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Gemini API request was cancelled by user");
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Gemini API request timed out after {Timeout} seconds", _timeoutSeconds);
            throw new TimeoutException($"Gemini API request timed out after {_timeoutSeconds} seconds");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Gemini API");
            throw new InvalidOperationException("Failed to call Gemini API", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini API response");
            throw new InvalidOperationException("Failed to parse Gemini API response", ex);
        }
    }
}
