namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Interface for streaming pipeline results to clients.
/// Allows progressive updates as the pipeline executes.
/// </summary>
public interface IStreamingService
{
    /// <summary>
    /// Streams recipe generation progress to the client.
    /// </summary>
    /// <param name="requestId">Unique request identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of pipeline events</returns>
    IAsyncEnumerable<PipelineEvent> StreamRecipeGenerationAsync(
        string requestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams product recommendation progress to the client.
    /// </summary>
    /// <param name="requestId">Unique request identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of pipeline events</returns>
    IAsyncEnumerable<PipelineEvent> StreamProductRecommendationsAsync(
        string requestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a pipeline event for streaming.
    /// </summary>
    /// <param name="requestId">Request identifier</param>
    /// <param name="event">Pipeline event to publish</param>
    Task PublishEventAsync(string requestId, PipelineEvent @event);
}

/// <summary>
/// Represents an event in the pipeline execution.
/// </summary>
public class PipelineEvent
{
    public string RequestId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public PipelineStage Stage { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public bool IsComplete { get; set; }
    public bool IsError { get; set; }
}

/// <summary>
/// Pipeline execution stages.
/// </summary>
public enum PipelineStage
{
    Started,
    GeneratorStarted,
    GeneratorCompleted,
    ValidatorStarted,
    ValidatorCompleted,
    CimriSearchStarted,
    CimriSearchProgress,
    CimriSearchCompleted,
    Completed,
    Error
}
