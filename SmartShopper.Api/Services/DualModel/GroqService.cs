using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Implementation of IGroqService for fast generation using Groq (Llama 3).
/// </summary>
public class GroqService : IGroqService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GroqService> _logger;
    private readonly GroqOptions _options;
    private readonly int _timeoutSeconds;

    public GroqService(
        HttpClient httpClient,
        IOptions<GroqOptions> options,
        IOptions<DualModelVerificationOptions> verificationOptions,
        ILogger<GroqService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        _timeoutSeconds = verificationOptions?.Value?.GeneratorTimeoutSeconds ?? 10;

        // Configure HttpClient
        var baseUrl = _options.BaseUrl.TrimEnd('/') + "/";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);
    }

    public async Task<string> GenerateRecipeDraftAsync(
        List<string> userInventory,
        string? recipeType,
        CancellationToken cancellationToken)
    {
        if (userInventory == null || !userInventory.Any())
        {
            throw new ArgumentException("User inventory cannot be null or empty", nameof(userInventory));
        }

        _logger.LogInformation("Generating recipe draft with Groq for {Count} ingredients", userInventory.Count);

        var prompt = BuildRecipePrompt(userInventory, recipeType);
        var response = await CallGroqApiAsync(prompt, cancellationToken);

        _logger.LogInformation("Successfully generated recipe draft");
        return response;
    }

    public async Task<string> GenerateProductRecommendationsAsync(
        List<string> shoppingList,
        CancellationToken cancellationToken)
    {
        if (shoppingList == null || !shoppingList.Any())
        {
            throw new ArgumentException("Shopping list cannot be null or empty", nameof(shoppingList));
        }

        _logger.LogInformation("Generating product recommendations with Groq for {Count} items", shoppingList.Count);

        var prompt = BuildProductRecommendationsPrompt(shoppingList);
        var response = await CallGroqApiAsync(prompt, cancellationToken);

        _logger.LogInformation("Successfully generated product recommendations");
        return response;
    }

    private string BuildRecipePrompt(List<string> userInventory, string? recipeType)
    {
        var inventoryList = string.Join(", ", userInventory);
        var recipeTypeText = string.IsNullOrWhiteSpace(recipeType) ? "any" : recipeType;

        return $@"You are a creative chef. Generate a recipe using these ingredients: {inventoryList}.
Recipe type preference: {recipeTypeText}.

Output JSON format:
{{
  ""recipeName"": ""string"",
  ""ingredients"": [{{""name"": ""string"", ""quantity"": ""string"", ""unit"": ""string""}}],
  ""missingIngredients"": [{{""name"": ""string"", ""quantity"": ""string"", ""unit"": ""string""}}],
  ""steps"": [""string""],
  ""prepTime"": ""string"",
  ""cookTime"": ""string"",
  ""servings"": number
}}

Be creative but practical. List any missing ingredients needed. Return ONLY valid JSON, no additional text.";
    }

    private string BuildProductRecommendationsPrompt(List<string> shoppingList)
    {
        var itemsList = string.Join(", ", shoppingList);

        return $@"You are a shopping assistant. Recommend products for this shopping list: {itemsList}.

Output JSON format:
{{
  ""recommendations"": [
    {{
      ""productName"": ""string"",
      ""estimatedQuantity"": ""string"",
      ""reasoning"": ""string""
    }}
  ]
}}

Be specific with product names so they can be searched on e-commerce sites. Return ONLY valid JSON, no additional text.";
    }

    private async Task<string> CallGroqApiAsync(string prompt, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model = _options.Model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = _options.Temperature,
            max_tokens = _options.MaxTokens
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var response = await _httpClient.PostAsync("chat/completions", content, linkedCts.Token);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(linkedCts.Token);
            var responseJson = JsonDocument.Parse(responseContent);

            var generatedText = responseJson.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(generatedText))
            {
                throw new InvalidOperationException("Groq API returned empty response");
            }

            return generatedText.Trim();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Groq API request was cancelled by user");
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Groq API request timed out after {Timeout} seconds", _timeoutSeconds);
            throw new TimeoutException($"Groq API request timed out after {_timeoutSeconds} seconds");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Groq API");
            throw new InvalidOperationException("Failed to call Groq API", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Groq API response");
            throw new InvalidOperationException("Failed to parse Groq API response", ex);
        }
    }
}
