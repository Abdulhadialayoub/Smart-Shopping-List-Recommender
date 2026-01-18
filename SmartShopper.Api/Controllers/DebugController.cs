using Microsoft.AspNetCore.Mvc;
using SmartShopper.Api.Services.DualModel;

namespace SmartShopper.Api.Controllers;

/// <summary>
/// Debug endpoints for monitoring and troubleshooting the dual-model verification pipeline.
/// WARNING: These endpoints should be secured with admin authentication in production.
/// </summary>
[ApiController]
[Route("api/ai/debug")]
public class DebugController : ControllerBase
{
    private readonly ILogger<DebugController> _logger;

    public DebugController(ILogger<DebugController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves the full pipeline execution log for a specific request.
    /// </summary>
    /// <param name="requestId">The unique request ID from a pipeline execution</param>
    /// <returns>Complete pipeline execution trace including prompts, responses, and timing</returns>
    /// <response code="200">Returns the pipeline execution log</response>
    /// <response code="404">Request ID not found</response>
    [HttpGet("pipeline/{requestId}")]
    [ProducesResponseType(typeof(PipelineExecutionLog), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetPipelineTrace(string requestId)
    {
        _logger.LogInformation("Debug: Retrieving pipeline trace for request {RequestId}", requestId);

        var log = DualModelVerificationService.GetPipelineLog(requestId);
        
        if (log == null)
        {
            _logger.LogWarning("Debug: Pipeline trace not found for request {RequestId}", requestId);
            return NotFound(new { error = $"Pipeline execution log not found for request ID: {requestId}" });
        }

        return Ok(log);
    }

    /// <summary>
    /// Retrieves all stored pipeline execution logs.
    /// </summary>
    /// <returns>List of all pipeline execution logs</returns>
    /// <response code="200">Returns all pipeline execution logs</response>
    [HttpGet("pipeline")]
    [ProducesResponseType(typeof(List<PipelineExecutionLog>), StatusCodes.Status200OK)]
    public IActionResult GetAllPipelineTraces()
    {
        _logger.LogInformation("Debug: Retrieving all pipeline traces");

        var logs = DualModelVerificationService.GetAllPipelineLogs();
        
        return Ok(new
        {
            count = logs.Count,
            logs = logs.OrderByDescending(l => l.Timestamp).ToList()
        });
    }

    /// <summary>
    /// Retrieves pipeline execution statistics.
    /// </summary>
    /// <returns>Aggregated statistics about pipeline executions</returns>
    /// <response code="200">Returns pipeline statistics</response>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetPipelineStats()
    {
        _logger.LogInformation("Debug: Retrieving pipeline statistics");

        var logs = DualModelVerificationService.GetAllPipelineLogs();
        
        if (!logs.Any())
        {
            return Ok(new { message = "No pipeline executions recorded yet" });
        }

        var stats = new
        {
            totalExecutions = logs.Count,
            successfulExecutions = logs.Count(l => l.Success),
            failedExecutions = logs.Count(l => !l.Success),
            successRate = logs.Count > 0 ? (double)logs.Count(l => l.Success) / logs.Count * 100 : 0,
            
            byPipelineType = logs.GroupBy(l => l.PipelineType)
                .Select(g => new
                {
                    type = g.Key,
                    count = g.Count(),
                    successRate = g.Count() > 0 ? (double)g.Count(l => l.Success) / g.Count() * 100 : 0
                })
                .ToList(),
            
            averageTimings = new
            {
                generatorMs = logs.Where(l => l.GeneratorResponseTimeMs > 0)
                    .Average(l => (double?)l.GeneratorResponseTimeMs) ?? 0,
                validatorMs = logs.Where(l => l.ValidatorResponseTimeMs.HasValue && l.ValidatorResponseTimeMs > 0)
                    .Average(l => (double?)l.ValidatorResponseTimeMs) ?? 0,
                totalPipelineMs = logs.Where(l => l.TotalPipelineTimeMs > 0)
                    .Average(l => (double?)l.TotalPipelineTimeMs) ?? 0
            },
            
            validationStats = new
            {
                totalValidated = logs.Count(l => l.WasValidated),
                validationRate = logs.Count > 0 ? (double)logs.Count(l => l.WasValidated) / logs.Count * 100 : 0,
                totalCorrections = logs.Sum(l => l.Corrections?.Count ?? 0),
                averageCorrectionsPerValidation = logs.Count(l => l.WasValidated) > 0 
                    ? (double)logs.Sum(l => l.Corrections?.Count ?? 0) / logs.Count(l => l.WasValidated) 
                    : 0
            },
            
            cacheStats = new
            {
                totalCacheHits = logs.Count(l => l.CacheHit),
                cacheHitRate = logs.Count > 0 ? (double)logs.Count(l => l.CacheHit) / logs.Count * 100 : 0
            },
            
            recentErrors = logs.Where(l => !l.Success)
                .OrderByDescending(l => l.Timestamp)
                .Take(5)
                .Select(l => new
                {
                    requestId = l.RequestId,
                    timestamp = l.Timestamp,
                    pipelineType = l.PipelineType,
                    errorMessage = l.ErrorMessage
                })
                .ToList()
        };

        return Ok(stats);
    }

    /// <summary>
    /// Health check endpoint for the debug API.
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            message = "Debug API is operational"
        });
    }

    /// <summary>
    /// Tests if OpenAI service is properly configured and working.
    /// </summary>
    [HttpGet("test-openai")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TestOpenAI([FromServices] IOpenAIService? openAIService)
    {
        if (openAIService == null)
        {
            return Ok(new { Status = "FAILED", Message = "OpenAI service is NULL - not injected by DI" });
        }

        try
        {
            var testJson = "{\"recipe\":{\"name\":\"Test\",\"ingredients\":[]}}";
            var result = await openAIService.ValidateAndCorrectRecipeAsync(
                testJson, 
                new List<string> { "test" }, 
                CancellationToken.None);
            
            return Ok(new 
            { 
                Status = "SUCCESS",
                Message = "OpenAI service is working correctly",
                ResponsePreview = result.Substring(0, Math.Min(200, result.Length))
            });
        }
        catch (Exception ex)
        {
            return Ok(new 
            { 
                Status = "FAILED",
                Message = "OpenAI service threw an exception",
                Error = ex.Message,
                ErrorType = ex.GetType().Name
            });
        }
    }

    /// <summary>
    /// Tests which validator is being used by DualModelVerificationService.
    /// </summary>
    [HttpGet("test-validator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult TestValidator(
        [FromServices] IDualModelVerificationService verificationService,
        [FromServices] IOpenAIService? openAIService,
        [FromServices] IGeminiService? geminiService)
    {
        return Ok(new
        {
            OpenAIServiceInjected = openAIService != null,
            GeminiServiceInjected = geminiService != null,
            Message = "Check the startup logs to see which validator was selected"
        });
    }
}

