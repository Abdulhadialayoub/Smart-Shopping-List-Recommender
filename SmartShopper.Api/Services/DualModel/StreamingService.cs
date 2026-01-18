using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace SmartShopper.Api.Services.DualModel;

/// <summary>
/// Implementation of streaming service for progressive pipeline updates.
/// Uses channels to stream events to clients in real-time.
/// </summary>
public class StreamingService : IStreamingService
{
    private readonly ILogger<StreamingService> _logger;
    
    // Store channels for each active request
    private readonly ConcurrentDictionary<string, Channel<PipelineEvent>> _channels = new();
    
    public StreamingService(ILogger<StreamingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async IAsyncEnumerable<PipelineEvent> StreamRecipeGenerationAsync(
        string requestId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = GetOrCreateChannel(requestId);
        
        _logger.LogInformation("Starting recipe generation stream for request: {RequestId}", requestId);
        
        await foreach (var @event in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return @event;
            
            if (@event.IsComplete || @event.IsError)
            {
                _logger.LogInformation("Recipe generation stream completed for request: {RequestId}", requestId);
                RemoveChannel(requestId);
                break;
            }
        }
    }

    public async IAsyncEnumerable<PipelineEvent> StreamProductRecommendationsAsync(
        string requestId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = GetOrCreateChannel(requestId);
        
        _logger.LogInformation("Starting product recommendations stream for request: {RequestId}", requestId);
        
        await foreach (var @event in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return @event;
            
            if (@event.IsComplete || @event.IsError)
            {
                _logger.LogInformation("Product recommendations stream completed for request: {RequestId}", requestId);
                RemoveChannel(requestId);
                break;
            }
        }
    }

    public async Task PublishEventAsync(string requestId, PipelineEvent @event)
    {
        var channel = GetOrCreateChannel(requestId);
        
        try
        {
            await channel.Writer.WriteAsync(@event);
            
            _logger.LogDebug("Published event for request {RequestId}: {Stage} - {Message}", 
                requestId, @event.Stage, @event.Message);
            
            // Close the channel if this is a completion or error event
            if (@event.IsComplete || @event.IsError)
            {
                channel.Writer.Complete();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event for request: {RequestId}", requestId);
        }
    }

    private Channel<PipelineEvent> GetOrCreateChannel(string requestId)
    {
        return _channels.GetOrAdd(requestId, _ => 
            Channel.CreateUnbounded<PipelineEvent>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            }));
    }

    private void RemoveChannel(string requestId)
    {
        if (_channels.TryRemove(requestId, out var channel))
        {
            channel.Writer.Complete();
        }
    }
}
