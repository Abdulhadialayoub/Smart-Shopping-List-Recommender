using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// OpenAI service for validating and correcting AI-generated content.
/// Acts as the validator in the dual-model verification pipeline.
/// Uses official OpenAI SDK.
/// </summary>
public class OpenAIService : IOpenAIService
{
    private readonly ChatClient _chatClient;
    private readonly OpenAIOptions _options;
    private readonly ILogger<OpenAIService> _logger;

    public OpenAIService(
        IOptions<OpenAIOptions> options,
        ILogger<OpenAIService> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize OpenAI ChatClient with API key
        _chatClient = new ChatClient(_options.Model, _options.ApiKey);
        
        _logger.LogInformation("OpenAI service initialized with model: {Model}", _options.Model);
    }

    public async Task<string> ValidateAndCorrectRecipeAsync(
        string draftRecipeJson,
        List<string> userInventory,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting OpenAI recipe validation");

        var prompt = BuildRecipeValidationPrompt(draftRecipeJson, userInventory);

        try
        {
            var response = await CallOpenAIAsync(prompt, cancellationToken);
            _logger.LogInformation("OpenAI recipe validation completed successfully");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI recipe validation failed");
            throw;
        }
    }

    public async Task<string> ValidateAndCorrectProductRecommendationsAsync(
        string draftRecommendationsJson,
        List<string> shoppingList,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting OpenAI product recommendations validation");

        var prompt = BuildProductValidationPrompt(draftRecommendationsJson, shoppingList);

        try
        {
            var response = await CallOpenAIAsync(prompt, cancellationToken);
            _logger.LogInformation("OpenAI product recommendations validation completed successfully");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI product recommendations validation failed");
            throw;
        }
    }

    private string BuildRecipeValidationPrompt(string draftRecipeJson, List<string> userInventory)
    {
        return $@"You are a food engineer performing quality control on a recipe.

USER INVENTORY: {string.Join(", ", userInventory)}

GENERATED RECIPE (JSON):
{draftRecipeJson}

VALIDATION CHECKLIST:
1. Are all recipe ingredients either in USER INVENTORY or listed in missingIngredients?
2. Are portion sizes realistic? (e.g., not ""5kg sugar"" for 4 servings)
3. Are cooking steps logically ordered?
4. Are cooking times and temperatures appropriate?
5. Is the missingIngredients list complete and accurate?

INSTRUCTIONS:
- If errors exist, correct them
- Return ONLY the corrected JSON in the same format
- If no errors, return the original JSON unchanged
- Do NOT add explanations or markdown formatting
- Output must be valid JSON only";
    }

    private string BuildProductValidationPrompt(string draftRecommendationsJson, List<string> shoppingList)
    {
        return $@"You are a quality control specialist for product recommendations.

SHOPPING LIST: {string.Join(", ", shoppingList)}

GENERATED RECOMMENDATIONS (JSON):
{draftRecommendationsJson}

VALIDATION CHECKLIST:
1. Are product names specific and searchable on e-commerce sites?
2. Are quantities realistic?
3. Are there any hallucinated or nonsensical products?
4. Does each recommendation have clear reasoning?

INSTRUCTIONS:
- If errors exist, correct them
- Return ONLY the corrected JSON in the same format
- If no errors, return the original JSON unchanged
- Do NOT add explanations or markdown formatting
- Output must be valid JSON only";
    }

    private async Task<string> CallOpenAIAsync(string prompt, CancellationToken cancellationToken)
    {
        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a precise JSON validator. Return only valid JSON without any markdown formatting or explanations."),
                new UserChatMessage(prompt)
            };

            var chatCompletionOptions = new ChatCompletionOptions
            {
                Temperature = (float)_options.Temperature,
                MaxOutputTokenCount = _options.MaxTokens
            };

            _logger.LogDebug("Sending request to OpenAI API");

            var completion = await _chatClient.CompleteChatAsync(messages, chatCompletionOptions, cancellationToken);

            if (completion?.Value?.Content == null || completion.Value.Content.Count == 0)
            {
                throw new InvalidOperationException("OpenAI returned empty response");
            }

            var result = completion.Value.Content[0].Text;

            _logger.LogDebug("Received response from OpenAI API");

            // Clean up markdown formatting if present
            result = result.Trim();
            if (result.StartsWith("```json"))
            {
                result = result.Substring(7);
            }
            if (result.StartsWith("```"))
            {
                result = result.Substring(3);
            }
            if (result.EndsWith("```"))
            {
                result = result.Substring(0, result.Length - 3);
            }
            result = result.Trim();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI API call failed");
            throw;
        }
    }
}
