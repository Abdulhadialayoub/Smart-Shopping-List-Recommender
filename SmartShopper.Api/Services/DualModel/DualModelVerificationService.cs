using System.Diagnostics;
using System.Text.Json;
using SmartShopper.Api.Models;

namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Orchestrates the dual-model verification pipeline.
/// Coordinates generation (Groq) and validation (Gemini/OpenAI) stages.
/// </summary>
public class DualModelVerificationService : IDualModelVerificationService
{
    private readonly IGroqService _groqService;
    private readonly IGeminiService? _geminiService;
    private readonly IOpenAIService? _openAIService;
    private readonly ICimriScraperService _cimriScraperService;
    private readonly ILogger<DualModelVerificationService> _logger;
    private readonly JsonRepairService _jsonRepairService;
    private readonly VerificationCacheService _cacheService;
    private readonly string _validatorName;
    
    // Configuration
    private readonly TimeSpan _generatorTimeout = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _validatorTimeout = TimeSpan.FromSeconds(15);
    private readonly TimeSpan _totalPipelineTimeout = TimeSpan.FromSeconds(20);
    
    // In-memory storage for pipeline execution logs (in production, use database or distributed cache)
    private static readonly Dictionary<string, PipelineExecutionLog> _executionLogs = new();
    private static readonly object _logLock = new();

    public DualModelVerificationService(
        IGroqService groqService,
        ICimriScraperService cimriScraperService,
        ILogger<DualModelVerificationService> logger,
        VerificationCacheService cacheService,
        IGeminiService? geminiService = null,
        IOpenAIService? openAIService = null)
    {
        _groqService = groqService ?? throw new ArgumentNullException(nameof(groqService));
        _cimriScraperService = cimriScraperService ?? throw new ArgumentNullException(nameof(cimriScraperService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        
        // At least one validator must be provided
        if (geminiService == null && openAIService == null)
        {
            throw new ArgumentException("At least one validator service (Gemini or OpenAI) must be provided");
        }
        
        _geminiService = geminiService;
        _openAIService = openAIService;
        
        // Prefer OpenAI if both are provided
        _validatorName = openAIService != null ? "OpenAI GPT-4" : "Gemini 2.0 Flash";
        
        _logger.LogInformation("DualModelVerificationService initialized with validator: {Validator}", _validatorName);
        
        _jsonRepairService = new JsonRepairService();
    }

    public async Task<VerifiedRecipeResponse> GenerateVerifiedRecipeAsync(
        List<string> userInventory,
        string? recipeType = null,
        CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString();
        var pipelineStopwatch = Stopwatch.StartNew();
        var metadata = new PipelineMetadata
        {
            Timestamp = DateTime.UtcNow,
            ValidatorModel = _validatorName
        };

        // Initialize pipeline execution log
        var executionLog = new PipelineExecutionLog
        {
            RequestId = requestId,
            Timestamp = DateTime.UtcNow,
            PipelineType = "Recipe",
            UserInventory = userInventory,
            RecipeType = recipeType
        };

        try
        {
            _logger.LogInformation("Starting recipe generation pipeline for {InventoryCount} items (RequestId: {RequestId})", 
                userInventory.Count, requestId);

            // Check cache first
            var cachedResponse = await _cacheService.GetCachedRecipeAsync(userInventory, recipeType);
            if (cachedResponse != null)
            {
                pipelineStopwatch.Stop();
                
                // Update metadata to indicate cache hit
                cachedResponse.Metadata.CacheHit = true;
                cachedResponse.Metadata.CacheKey = _cacheService.GenerateRecipeCacheKey(userInventory, recipeType);
                cachedResponse.Metadata.TotalPipelineTimeMs = (int)pipelineStopwatch.ElapsedMilliseconds;
                
                _logger.LogInformation("Returning cached recipe response in {Ms}ms (RequestId: {RequestId})", 
                    cachedResponse.Metadata.TotalPipelineTimeMs, requestId);
                
                // Log cache hit
                executionLog.Success = true;
                executionLog.CacheHit = true;
                executionLog.TotalPipelineTimeMs = (int)pipelineStopwatch.ElapsedMilliseconds;
                StorePipelineLog(executionLog);
                
                return cachedResponse;
            }

            metadata.CacheHit = false;
            metadata.CacheKey = _cacheService.GenerateRecipeCacheKey(userInventory, recipeType);
            executionLog.CacheHit = false;

            // Stage 1: Generate draft recipe using Groq
            var generatorStopwatch = Stopwatch.StartNew();
            string draftRecipeJson;
            
            try
            {
                using var generatorCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                generatorCts.CancelAfter(_generatorTimeout);
                
                // Build and log generator prompt
                executionLog.GeneratorPrompt = $"Generate recipe for inventory: {string.Join(", ", userInventory)}. Type: {recipeType ?? "any"}";
                
                draftRecipeJson = await _groqService.GenerateRecipeDraftAsync(
                    userInventory, 
                    recipeType, 
                    generatorCts.Token);
                
                generatorStopwatch.Stop();
                metadata.GeneratorResponseTimeMs = (int)generatorStopwatch.ElapsedMilliseconds;
                executionLog.GeneratorResponse = draftRecipeJson;
                executionLog.GeneratorResponseTimeMs = metadata.GeneratorResponseTimeMs;
                
                _logger.LogInformation("Generator completed in {Ms}ms (RequestId: {RequestId})", 
                    metadata.GeneratorResponseTimeMs, requestId);
            }
            catch (OperationCanceledException)
            {
                executionLog.Success = false;
                executionLog.ErrorMessage = $"Generator timeout after {_generatorTimeout.TotalSeconds} seconds";
                executionLog.TotalPipelineTimeMs = (int)pipelineStopwatch.ElapsedMilliseconds;
                StorePipelineLog(executionLog);
                
                _logger.LogError("Generator timeout after {Timeout}s (RequestId: {RequestId})", 
                    _generatorTimeout.TotalSeconds, requestId);
                throw new TimeoutException($"Recipe generation timed out after {_generatorTimeout.TotalSeconds} seconds");
            }

            // Stage 2: Validate and correct using Gemini
            string validatedRecipeJson = draftRecipeJson;
            var validatorStopwatch = Stopwatch.StartNew();
            
            try
            {
                using var validatorCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                validatorCts.CancelAfter(_validatorTimeout);
                
                // Build and log validator prompt
                executionLog.ValidatorPrompt = $"Validate recipe for inventory: {string.Join(", ", userInventory)}";
                
                validatedRecipeJson = await ValidateRecipeAsync(
                    draftRecipeJson,
                    userInventory,
                    validatorCts.Token);
                
                validatorStopwatch.Stop();
                metadata.ValidatorResponseTimeMs = (int)validatorStopwatch.ElapsedMilliseconds;
                metadata.WasValidated = true;
                executionLog.ValidatorResponse = validatedRecipeJson;
                executionLog.ValidatorResponseTimeMs = metadata.ValidatorResponseTimeMs;
                executionLog.WasValidated = true;
                
                _logger.LogInformation("Validator completed in {Ms}ms (RequestId: {RequestId})", 
                    metadata.ValidatorResponseTimeMs, requestId);
                
                // Detect corrections by comparing draft and validated JSON
                DetectCorrections(draftRecipeJson, validatedRecipeJson, metadata);
                executionLog.Corrections = metadata.Corrections;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Validator timeout after {Timeout}s, using generator output (RequestId: {RequestId})", 
                    _validatorTimeout.TotalSeconds, requestId);
                metadata.ValidatorResponseTimeMs = (int)validatorStopwatch.ElapsedMilliseconds;
                metadata.WasValidated = false;
                metadata.Corrections.Add("Validator timed out - using generator output");
                executionLog.ValidatorResponseTimeMs = metadata.ValidatorResponseTimeMs;
                executionLog.WasValidated = false;
                executionLog.Corrections = metadata.Corrections;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validator failed, using generator output (RequestId: {RequestId})", requestId);
                metadata.ValidatorResponseTimeMs = (int)validatorStopwatch.ElapsedMilliseconds;
                metadata.WasValidated = false;
                metadata.Corrections.Add($"Validator failed: {ex.Message} - using generator output");
                executionLog.ValidatorResponseTimeMs = metadata.ValidatorResponseTimeMs;
                executionLog.WasValidated = false;
                executionLog.Corrections = metadata.Corrections;
                executionLog.ErrorMessage = $"Validator error: {ex.Message}";
            }

            // Parse the final JSON
            VerifiedRecipeResponse response;
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            try
            {
                response = JsonSerializer.Deserialize<VerifiedRecipeResponse>(validatedRecipeJson, jsonOptions)
                    ?? throw new InvalidOperationException("Deserialization returned null");
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse JSON, attempting repair (RequestId: {RequestId})", requestId);
                
                // Attempt JSON repair
                var repairedJson = _jsonRepairService.RepairJson(validatedRecipeJson);
                try
                {
                    response = JsonSerializer.Deserialize<VerifiedRecipeResponse>(repairedJson, jsonOptions)
                        ?? throw new InvalidOperationException("Deserialization returned null after repair");
                    
                    _logger.LogInformation("JSON repair successful (RequestId: {RequestId})", requestId);
                    executionLog.Corrections.Add("JSON repair applied");
                }
                catch (JsonException)
                {
                    executionLog.Success = false;
                    executionLog.ErrorMessage = "Failed to parse recipe JSON even after repair attempt";
                    executionLog.TotalPipelineTimeMs = (int)pipelineStopwatch.ElapsedMilliseconds;
                    StorePipelineLog(executionLog);
                    
                    _logger.LogError("JSON repair failed, returning error (RequestId: {RequestId})", requestId);
                    throw new InvalidOperationException("Failed to parse recipe JSON even after repair attempt");
                }
            }

            pipelineStopwatch.Stop();
            metadata.TotalPipelineTimeMs = (int)pipelineStopwatch.ElapsedMilliseconds;
            response.Metadata = metadata;

            // Complete execution log
            executionLog.Success = true;
            executionLog.TotalPipelineTimeMs = metadata.TotalPipelineTimeMs;
            executionLog.MissingIngredientsCount = response.MissingIngredients?.Count;
            StorePipelineLog(executionLog);

            // Store in cache for future requests
            await _cacheService.SetCachedRecipeAsync(userInventory, recipeType, response);

            _logger.LogInformation("Recipe generation pipeline completed in {Ms}ms (RequestId: {RequestId})", 
                metadata.TotalPipelineTimeMs, requestId);
            
            return response;
        }
        catch (Exception ex)
        {
            pipelineStopwatch.Stop();
            metadata.TotalPipelineTimeMs = (int)pipelineStopwatch.ElapsedMilliseconds;
            
            executionLog.Success = false;
            executionLog.ErrorMessage = ex.Message;
            executionLog.TotalPipelineTimeMs = (int)pipelineStopwatch.ElapsedMilliseconds;
            StorePipelineLog(executionLog);
            
            _logger.LogError(ex, "Recipe generation pipeline failed after {Ms}ms (RequestId: {RequestId})", 
                metadata.TotalPipelineTimeMs, requestId);
            throw;
        }
    }

    public async Task<VerifiedProductResponse> GenerateVerifiedProductRecommendationsAsync(
        List<string> shoppingList,
        CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString();
        var pipelineStopwatch = Stopwatch.StartNew();
        var metadata = new PipelineMetadata
        {
            Timestamp = DateTime.UtcNow,
            ValidatorModel = _validatorName
        };

        // Initialize pipeline execution log
        var executionLog = new PipelineExecutionLog
        {
            RequestId = requestId,
            Timestamp = DateTime.UtcNow,
            PipelineType = "Product",
            ShoppingList = shoppingList
        };

        try
        {
            _logger.LogInformation("Starting product recommendations pipeline for {ItemCount} items (RequestId: {RequestId})", 
                shoppingList.Count, requestId);

            // Check cache first
            var cachedResponse = await _cacheService.GetCachedProductsAsync(shoppingList);
            if (cachedResponse != null)
            {
                pipelineStopwatch.Stop();
                
                // Update metadata to indicate cache hit
                cachedResponse.Metadata.CacheHit = true;
                cachedResponse.Metadata.CacheKey = _cacheService.GenerateProductCacheKey(shoppingList);
                cachedResponse.Metadata.TotalPipelineTimeMs = (int)pipelineStopwatch.ElapsedMilliseconds;
                
                _logger.LogInformation("Returning cached product recommendations in {Ms}ms (RequestId: {RequestId})", 
                    cachedResponse.Metadata.TotalPipelineTimeMs, requestId);
                
                // Log cache hit
                executionLog.Success = true;
                executionLog.CacheHit = true;
                executionLog.TotalPipelineTimeMs = (int)pipelineStopwatch.ElapsedMilliseconds;
                StorePipelineLog(executionLog);
                
                return cachedResponse;
            }

            metadata.CacheHit = false;
            metadata.CacheKey = _cacheService.GenerateProductCacheKey(shoppingList);
            executionLog.CacheHit = false;

            // Stage 1: Generate draft recommendations using Groq
            var generatorStopwatch = Stopwatch.StartNew();
            string draftRecommendationsJson;
            
            try
            {
                using var generatorCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                generatorCts.CancelAfter(_generatorTimeout);
                
                // Build and log generator prompt
                executionLog.GeneratorPrompt = $"Generate product recommendations for: {string.Join(", ", shoppingList)}";
                
                draftRecommendationsJson = await _groqService.GenerateProductRecommendationsAsync(
                    shoppingList, 
                    generatorCts.Token);
                
                generatorStopwatch.Stop();
                metadata.GeneratorResponseTimeMs = (int)generatorStopwatch.ElapsedMilliseconds;
                executionLog.GeneratorResponse = draftRecommendationsJson;
                executionLog.GeneratorResponseTimeMs = metadata.GeneratorResponseTimeMs;
                
                _logger.LogInformation("Generator completed in {Ms}ms (RequestId: {RequestId})", 
                    metadata.GeneratorResponseTimeMs, requestId);
            }
            catch (OperationCanceledException)
            {
                executionLog.Success = false;
                executionLog.ErrorMessage = $"Generator timeout after {_generatorTimeout.TotalSeconds} seconds";
                executionLog.TotalPipelineTimeMs = (int)pipelineStopwatch.ElapsedMilliseconds;
                StorePipelineLog(executionLog);
                
                _logger.LogError("Generator timeout after {Timeout}s (RequestId: {RequestId})", 
                    _generatorTimeout.TotalSeconds, requestId);
                throw new TimeoutException($"Product recommendations generation timed out after {_generatorTimeout.TotalSeconds} seconds");
            }

            // Stage 2: Validate and correct using Gemini
            string validatedRecommendationsJson = draftRecommendationsJson;
            var validatorStopwatch = Stopwatch.StartNew();
            
            try
            {
                using var validatorCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                validatorCts.CancelAfter(_validatorTimeout);
                
                // Build and log validator prompt
                executionLog.ValidatorPrompt = $"Validate product recommendations for: {string.Join(", ", shoppingList)}";
                
                validatedRecommendationsJson = await ValidateProductRecommendationsAsync(
                    draftRecommendationsJson,
                    shoppingList,
                    validatorCts.Token);
                
                validatorStopwatch.Stop();
                metadata.ValidatorResponseTimeMs = (int)validatorStopwatch.ElapsedMilliseconds;
                metadata.WasValidated = true;
                executionLog.ValidatorResponse = validatedRecommendationsJson;
                executionLog.ValidatorResponseTimeMs = metadata.ValidatorResponseTimeMs;
                executionLog.WasValidated = true;
                
                _logger.LogInformation("Validator completed in {Ms}ms (RequestId: {RequestId})", 
                    metadata.ValidatorResponseTimeMs, requestId);
                
                // Detect corrections by comparing draft and validated JSON
                DetectCorrections(draftRecommendationsJson, validatedRecommendationsJson, metadata);
                executionLog.Corrections = metadata.Corrections;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Validator timeout after {Timeout}s, using generator output (RequestId: {RequestId})", 
                    _validatorTimeout.TotalSeconds, requestId);
                metadata.ValidatorResponseTimeMs = (int)validatorStopwatch.ElapsedMilliseconds;
                metadata.WasValidated = false;
                metadata.Corrections.Add("Validator timed out - using generator output");
                executionLog.ValidatorResponseTimeMs = metadata.ValidatorResponseTimeMs;
                executionLog.WasValidated = false;
                executionLog.Corrections = metadata.Corrections;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validator failed, using generator output (RequestId: {RequestId})", requestId);
                metadata.ValidatorResponseTimeMs = (int)validatorStopwatch.ElapsedMilliseconds;
                metadata.WasValidated = false;
                metadata.Corrections.Add($"Validator failed: {ex.Message} - using generator output");
                executionLog.ValidatorResponseTimeMs = metadata.ValidatorResponseTimeMs;
                executionLog.WasValidated = false;
                executionLog.Corrections = metadata.Corrections;
                executionLog.ErrorMessage = $"Validator error: {ex.Message}";
            }

            // Parse the final JSON
            VerifiedProductResponse response;
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            try
            {
                response = JsonSerializer.Deserialize<VerifiedProductResponse>(validatedRecommendationsJson, jsonOptions)
                    ?? throw new InvalidOperationException("Deserialization returned null");
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse JSON, attempting repair (RequestId: {RequestId})", requestId);
                
                // Attempt JSON repair
                var repairedJson = _jsonRepairService.RepairJson(validatedRecommendationsJson);
                try
                {
                    response = JsonSerializer.Deserialize<VerifiedProductResponse>(repairedJson, jsonOptions)
                        ?? throw new InvalidOperationException("Deserialization returned null after repair");
                    
                    _logger.LogInformation("JSON repair successful (RequestId: {RequestId})", requestId);
                    executionLog.Corrections.Add("JSON repair applied");
                }
                catch (JsonException)
                {
                    executionLog.Success = false;
                    executionLog.ErrorMessage = "Failed to parse product recommendations JSON even after repair attempt";
                    executionLog.TotalPipelineTimeMs = (int)pipelineStopwatch.ElapsedMilliseconds;
                    StorePipelineLog(executionLog);
                    
                    _logger.LogError("JSON repair failed, returning error (RequestId: {RequestId})", requestId);
                    throw new InvalidOperationException("Failed to parse product recommendations JSON even after repair attempt");
                }
            }

            pipelineStopwatch.Stop();
            metadata.TotalPipelineTimeMs = (int)pipelineStopwatch.ElapsedMilliseconds;
            response.Metadata = metadata;

            // Complete execution log
            executionLog.Success = true;
            executionLog.TotalPipelineTimeMs = metadata.TotalPipelineTimeMs;
            StorePipelineLog(executionLog);

            // Store in cache for future requests
            await _cacheService.SetCachedProductsAsync(shoppingList, response);

            _logger.LogInformation("Product recommendations pipeline completed in {Ms}ms (RequestId: {RequestId})", 
                metadata.TotalPipelineTimeMs, requestId);
            
            return response;
        }
        catch (Exception ex)
        {
            pipelineStopwatch.Stop();
            metadata.TotalPipelineTimeMs = (int)pipelineStopwatch.ElapsedMilliseconds;
            
            executionLog.Success = false;
            executionLog.ErrorMessage = ex.Message;
            executionLog.TotalPipelineTimeMs = (int)pipelineStopwatch.ElapsedMilliseconds;
            StorePipelineLog(executionLog);
            
            _logger.LogError(ex, "Product recommendations pipeline failed after {Ms}ms (RequestId: {RequestId})", 
                metadata.TotalPipelineTimeMs, requestId);
            throw;
        }
    }

    public async Task<VerifiedRecipeWithPricesResponse> GenerateVerifiedRecipeWithPricesAsync(
        List<string> userInventory,
        string? recipeType = null,
        CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString();
        var pipelineStopwatch = Stopwatch.StartNew();
        
        // Initialize pipeline execution log
        var executionLog = new PipelineExecutionLog
        {
            RequestId = requestId,
            Timestamp = DateTime.UtcNow,
            PipelineType = "RecipeWithPrices",
            UserInventory = userInventory,
            RecipeType = recipeType
        };
        
        try
        {
            _logger.LogInformation("Starting recipe with prices pipeline (RequestId: {RequestId})", requestId);

            // Stage 1 & 2: Generate and validate recipe
            var recipe = await GenerateVerifiedRecipeAsync(userInventory, recipeType, cancellationToken);
            
            _logger.LogInformation("Recipe generated, searching for {Count} missing ingredients (RequestId: {RequestId})", 
                recipe.MissingIngredients.Count, requestId);

            // Stage 3: Search Cimri for missing ingredients in parallel
            var cimriStopwatch = Stopwatch.StartNew();
            var productPrices = new Dictionary<string, List<CimriProduct>>();
            
            if (recipe.MissingIngredients.Any())
            {
                _logger.LogInformation("Searching Cimri for {Count} missing ingredients", 
                    recipe.MissingIngredients.Count);
                
                // Use parallel search with individual timeouts for each search
                productPrices = await SearchCimriInParallelAsync(
                    recipe.MissingIngredients.Select(i => i.Name).ToList(),
                    requestId,
                    cancellationToken);
                
                cimriStopwatch.Stop();
                executionLog.CimriSearchTimeMs = (int)cimriStopwatch.ElapsedMilliseconds;
                
                var successfulSearches = productPrices.Count(p => p.Value.Any());
                var failedSearches = productPrices.Count - successfulSearches;
                
                _logger.LogInformation("Cimri searches completed in {Ms}ms: {Success} successful, {Failed} failed (RequestId: {RequestId})", 
                    cimriStopwatch.ElapsedMilliseconds, successfulSearches, failedSearches, requestId);
            }
            else
            {
                _logger.LogInformation("No missing ingredients, skipping Cimri search (RequestId: {RequestId})", requestId);
            }

            pipelineStopwatch.Stop();
            
            // Update metadata with total time including Cimri searches
            recipe.Metadata.TotalPipelineTimeMs = (int)pipelineStopwatch.ElapsedMilliseconds;

            var response = new VerifiedRecipeWithPricesResponse
            {
                Recipe = recipe,
                ProductPrices = productPrices,
                Metadata = recipe.Metadata
            };

            // Complete execution log
            executionLog.Success = true;
            executionLog.TotalPipelineTimeMs = (int)pipelineStopwatch.ElapsedMilliseconds;
            executionLog.MissingIngredientsCount = recipe.MissingIngredients?.Count;
            executionLog.WasValidated = recipe.Metadata.WasValidated;
            executionLog.GeneratorResponseTimeMs = recipe.Metadata.GeneratorResponseTimeMs;
            executionLog.ValidatorResponseTimeMs = recipe.Metadata.ValidatorResponseTimeMs;
            executionLog.Corrections = recipe.Metadata.Corrections;
            StorePipelineLog(executionLog);

            _logger.LogInformation("Recipe with prices pipeline completed in {Ms}ms (RequestId: {RequestId})", 
                response.Metadata.TotalPipelineTimeMs, requestId);
            
            return response;
        }
        catch (Exception ex)
        {
            pipelineStopwatch.Stop();
            
            executionLog.Success = false;
            executionLog.ErrorMessage = ex.Message;
            executionLog.TotalPipelineTimeMs = (int)pipelineStopwatch.ElapsedMilliseconds;
            StorePipelineLog(executionLog);
            
            _logger.LogError(ex, "Recipe with prices pipeline failed after {Ms}ms (RequestId: {RequestId})", 
                pipelineStopwatch.ElapsedMilliseconds, requestId);
            throw;
        }
    }

    /// <summary>
    /// Detects corrections made by the validator by comparing draft and validated JSON.
    /// </summary>
    private void DetectCorrections(string draftJson, string validatedJson, PipelineMetadata metadata)
    {
        try
        {
            if (draftJson.Trim() == validatedJson.Trim())
            {
                _logger.LogInformation("No corrections detected - outputs are identical");
                return;
            }

            // Simple heuristic: if JSONs differ significantly, corrections were made
            var draftLength = draftJson.Length;
            var validatedLength = validatedJson.Length;
            var lengthDiff = Math.Abs(draftLength - validatedLength);
            
            if (lengthDiff > 10)
            {
                metadata.Corrections.Add($"Content modified (length changed by {lengthDiff} characters)");
                _logger.LogInformation("Corrections detected: length changed by {Diff} characters", lengthDiff);
            }
            else
            {
                metadata.Corrections.Add("Minor corrections applied");
                _logger.LogInformation("Minor corrections detected");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect corrections");
        }
    }

    /// <summary>
    /// Calls the appropriate validator service (OpenAI or Gemini) for recipe validation.
    /// </summary>
    private async Task<string> ValidateRecipeAsync(
        string draftRecipeJson,
        List<string> userInventory,
        CancellationToken cancellationToken)
    {
        if (_openAIService != null)
        {
            _logger.LogDebug("Using OpenAI for recipe validation");
            return await _openAIService.ValidateAndCorrectRecipeAsync(
                draftRecipeJson,
                userInventory,
                cancellationToken);
        }
        else if (_geminiService != null)
        {
            _logger.LogDebug("Using Gemini for recipe validation");
            return await _geminiService.ValidateAndCorrectRecipeAsync(
                draftRecipeJson,
                userInventory,
                cancellationToken);
        }
        
        throw new InvalidOperationException("No validator service available");
    }

    /// <summary>
    /// Calls the appropriate validator service (OpenAI or Gemini) for product recommendations validation.
    /// </summary>
    private async Task<string> ValidateProductRecommendationsAsync(
        string draftRecommendationsJson,
        List<string> shoppingList,
        CancellationToken cancellationToken)
    {
        if (_openAIService != null)
        {
            _logger.LogDebug("Using OpenAI for product recommendations validation");
            return await _openAIService.ValidateAndCorrectProductRecommendationsAsync(
                draftRecommendationsJson,
                shoppingList,
                cancellationToken);
        }
        else if (_geminiService != null)
        {
            _logger.LogDebug("Using Gemini for product recommendations validation");
            return await _geminiService.ValidateAndCorrectProductRecommendationsAsync(
                draftRecommendationsJson,
                shoppingList,
                cancellationToken);
        }
        
        throw new InvalidOperationException("No validator service available");
    }

    /// <summary>
    /// Searches Cimri for multiple products in parallel with individual timeouts.
    /// Handles partial failures gracefully by returning empty lists for failed searches.
    /// </summary>
    /// <param name="productNames">List of product names to search for</param>
    /// <param name="requestId">Request ID for logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping product names to their search results</returns>
    private async Task<Dictionary<string, List<CimriProduct>>> SearchCimriInParallelAsync(
        List<string> productNames,
        string requestId,
        CancellationToken cancellationToken)
    {
        // Individual search timeout (5 seconds per search)
        var searchTimeout = TimeSpan.FromSeconds(5);
        
        var searchTasks = productNames
            .Select(async productName =>
            {
                try
                {
                    _logger.LogDebug("Searching Cimri for: {Product} (RequestId: {RequestId})", 
                        productName, requestId);
                    
                    // Create a timeout token for this individual search
                    using var searchCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    searchCts.CancelAfter(searchTimeout);
                    
                    // Wrap the search in a Task.Run to ensure timeout is respected
                    var searchTask = Task.Run(async () =>
                    {
                        var searchResult = await _cimriScraperService.SearchProductsAsync(
                            productName,
                            page: 1,
                            sort: "");
                        
                        // Take top 3 products
                        return searchResult.Products.Take(3).ToList();
                    }, searchCts.Token);
                    
                    var products = await searchTask;
                    
                    _logger.LogDebug("Found {Count} products for: {Product} (RequestId: {RequestId})", 
                        products.Count, productName, requestId);
                    
                    return new { ProductName = productName, Products = products, Success = true };
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Cimri search timeout for: {Product} after {Timeout}s (RequestId: {RequestId})", 
                        productName, searchTimeout.TotalSeconds, requestId);
                    return new { ProductName = productName, Products = new List<CimriProduct>(), Success = false };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to search Cimri for: {Product} (RequestId: {RequestId})", 
                        productName, requestId);
                    return new { ProductName = productName, Products = new List<CimriProduct>(), Success = false };
                }
            })
            .ToList();

        // Wait for all searches to complete (or timeout/fail)
        var searchResults = await Task.WhenAll(searchTasks);
        
        // Build result dictionary
        var productPrices = new Dictionary<string, List<CimriProduct>>();
        foreach (var result in searchResults)
        {
            productPrices[result.ProductName] = result.Products;
        }
        
        return productPrices;
    }

    /// <summary>
    /// Stores a pipeline execution log for debugging and monitoring.
    /// </summary>
    private void StorePipelineLog(PipelineExecutionLog log)
    {
        try
        {
            lock (_logLock)
            {
                _executionLogs[log.RequestId] = log;
                
                // Keep only the last 1000 logs to prevent memory issues
                if (_executionLogs.Count > 1000)
                {
                    var oldestKey = _executionLogs.Keys.First();
                    _executionLogs.Remove(oldestKey);
                }
            }

            // Log as structured JSON for external log aggregation systems
            _logger.LogInformation("Pipeline execution log stored: {LogJson}", 
                JsonSerializer.Serialize(log));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store pipeline execution log");
        }
    }

    /// <summary>
    /// Retrieves a pipeline execution log by request ID.
    /// </summary>
    public static PipelineExecutionLog? GetPipelineLog(string requestId)
    {
        lock (_logLock)
        {
            return _executionLogs.TryGetValue(requestId, out var log) ? log : null;
        }
    }

    /// <summary>
    /// Gets all stored pipeline execution logs.
    /// </summary>
    public static List<PipelineExecutionLog> GetAllPipelineLogs()
    {
        lock (_logLock)
        {
            return _executionLogs.Values.ToList();
        }
    }
}
