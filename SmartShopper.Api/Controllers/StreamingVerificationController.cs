using Microsoft.AspNetCore.Mvc;
using SmartShopper.Api.Services.DualModel;

namespace SmartShopper.Api.Controllers;

/// <summary>
/// Controller for streaming verification pipeline results.
/// Provides Server-Sent Events (SSE) endpoints for real-time updates.
/// </summary>
[ApiController]
[Route("api/streaming")]
public class StreamingVerificationController : ControllerBase
{
    private readonly IStreamingService _streamingService;
    private readonly ILogger<StreamingVerificationController> _logger;

    public StreamingVerificationController(
        IStreamingService streamingService,
        ILogger<StreamingVerificationController> logger)
    {
        _streamingService = streamingService ?? throw new ArgumentNullException(nameof(streamingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Streams recipe generation progress using Server-Sent Events.
    /// </summary>
    /// <param name="requestId">The request ID to stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SSE stream of pipeline events</returns>
    [HttpGet("recipe/{requestId}")]
    [Produces("text/event-stream")]
    public async Task StreamRecipeGeneration(
        string requestId,
        CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        _logger.LogInformation("Client connected to recipe stream: {RequestId}", requestId);

        try
        {
            await foreach (var @event in _streamingService.StreamRecipeGenerationAsync(requestId, cancellationToken))
            {
                var eventData = System.Text.Json.JsonSerializer.Serialize(@event);
                await Response.WriteAsync($"data: {eventData}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Client disconnected from recipe stream: {RequestId}", requestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming recipe generation: {RequestId}", requestId);
        }
    }

    /// <summary>
    /// Streams product recommendations progress using Server-Sent Events.
    /// </summary>
    /// <param name="requestId">The request ID to stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SSE stream of pipeline events</returns>
    [HttpGet("products/{requestId}")]
    [Produces("text/event-stream")]
    public async Task StreamProductRecommendations(
        string requestId,
        CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        _logger.LogInformation("Client connected to product stream: {RequestId}", requestId);

        try
        {
            await foreach (var @event in _streamingService.StreamProductRecommendationsAsync(requestId, cancellationToken))
            {
                var eventData = System.Text.Json.JsonSerializer.Serialize(@event);
                await Response.WriteAsync($"data: {eventData}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Client disconnected from product stream: {RequestId}", requestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming product recommendations: {RequestId}", requestId);
        }
    }
}
