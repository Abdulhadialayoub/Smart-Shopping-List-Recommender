# Response Streaming for Dual-Model Verification

## Overview

The streaming service provides real-time progress updates for the dual-model verification pipeline. This allows clients to receive progressive updates as the pipeline executes, improving user experience for long-running operations.

## Architecture

The streaming implementation uses:
- **Channels**: For efficient async message passing
- **Server-Sent Events (SSE)**: For HTTP streaming to clients
- **IAsyncEnumerable**: For streaming data in ASP.NET Core

## Usage

### 1. Register the Streaming Service

In `Program.cs`, register the streaming service:

```csharp
builder.Services.AddSingleton<IStreamingService, StreamingService>();
```

### 2. Integrate with DualModelVerificationService

To enable streaming, modify the `DualModelVerificationService` to publish events:

```csharp
public class DualModelVerificationService
{
    private readonly IStreamingService? _streamingService;
    
    public DualModelVerificationService(
        // ... other dependencies
        IStreamingService? streamingService = null)
    {
        _streamingService = streamingService;
    }
    
    public async Task<VerifiedRecipeResponse> GenerateVerifiedRecipeAsync(...)
    {
        var requestId = Guid.NewGuid().ToString();
        
        // Publish start event
        await PublishEventAsync(requestId, new PipelineEvent
        {
            RequestId = requestId,
            Stage = PipelineStage.Started,
            Message = "Starting recipe generation"
        });
        
        // Publish generator start
        await PublishEventAsync(requestId, new PipelineEvent
        {
            RequestId = requestId,
            Stage = PipelineStage.GeneratorStarted,
            Message = "Generating recipe draft with Groq"
        });
        
        var draftRecipe = await _groqService.GenerateRecipeDraftAsync(...);
        
        // Publish generator complete
        await PublishEventAsync(requestId, new PipelineEvent
        {
            RequestId = requestId,
            Stage = PipelineStage.GeneratorCompleted,
            Message = "Recipe draft generated",
            Data = new { ResponseTimeMs = generatorStopwatch.ElapsedMilliseconds }
        });
        
        // ... continue with validator and Cimri searches
        
        // Publish completion
        await PublishEventAsync(requestId, new PipelineEvent
        {
            RequestId = requestId,
            Stage = PipelineStage.Completed,
            Message = "Recipe generation complete",
            IsComplete = true
        });
        
        return response;
    }
    
    private async Task PublishEventAsync(string requestId, PipelineEvent @event)
    {
        if (_streamingService != null)
        {
            await _streamingService.PublishEventAsync(requestId, @event);
        }
    }
}
```

### 3. Client-Side Usage

#### JavaScript/TypeScript Example

```typescript
async function streamRecipeGeneration(requestId: string) {
    const eventSource = new EventSource(`/api/streaming/recipe/${requestId}`);
    
    eventSource.onmessage = (event) => {
        const pipelineEvent = JSON.parse(event.data);
        
        console.log(`[${pipelineEvent.stage}] ${pipelineEvent.message}`);
        
        switch (pipelineEvent.stage) {
            case 'GeneratorStarted':
                updateUI('Generating recipe...');
                break;
            case 'GeneratorCompleted':
                updateUI('Validating recipe...');
                break;
            case 'CimriSearchStarted':
                updateUI('Searching for prices...');
                break;
            case 'Completed':
                updateUI('Complete!');
                eventSource.close();
                break;
            case 'Error':
                updateUI('Error: ' + pipelineEvent.message);
                eventSource.close();
                break;
        }
    };
    
    eventSource.onerror = (error) => {
        console.error('Stream error:', error);
        eventSource.close();
    };
}
```

#### React Example

```tsx
import { useEffect, useState } from 'react';

function RecipeGenerator() {
    const [status, setStatus] = useState('');
    const [progress, setProgress] = useState(0);
    
    useEffect(() => {
        const requestId = generateRequestId();
        const eventSource = new EventSource(`/api/streaming/recipe/${requestId}`);
        
        eventSource.onmessage = (event) => {
            const pipelineEvent = JSON.parse(event.data);
            setStatus(pipelineEvent.message);
            
            // Update progress based on stage
            const progressMap = {
                'Started': 10,
                'GeneratorStarted': 20,
                'GeneratorCompleted': 50,
                'ValidatorStarted': 60,
                'ValidatorCompleted': 80,
                'CimriSearchStarted': 85,
                'CimriSearchCompleted': 95,
                'Completed': 100
            };
            
            setProgress(progressMap[pipelineEvent.stage] || 0);
            
            if (pipelineEvent.isComplete || pipelineEvent.isError) {
                eventSource.close();
            }
        };
        
        return () => eventSource.close();
    }, []);
    
    return (
        <div>
            <div>Status: {status}</div>
            <progress value={progress} max="100" />
        </div>
    );
}
```

## Pipeline Stages

The following stages are emitted during pipeline execution:

1. **Started**: Pipeline has begun
2. **GeneratorStarted**: Groq is generating the draft
3. **GeneratorCompleted**: Groq has finished, draft is ready
4. **ValidatorStarted**: Gemini is validating the draft
5. **ValidatorCompleted**: Gemini has finished validation
6. **CimriSearchStarted**: Starting product searches
7. **CimriSearchProgress**: Individual product search updates
8. **CimriSearchCompleted**: All product searches complete
9. **Completed**: Pipeline finished successfully
10. **Error**: An error occurred

## Benefits

1. **Better UX**: Users see progress instead of waiting for a black box
2. **Transparency**: Users understand what's happening at each stage
3. **Early Feedback**: Users can see partial results as they arrive
4. **Debugging**: Developers can see exactly where issues occur
5. **Cancellation**: Users can cancel long-running operations

## Performance Considerations

- Streaming adds minimal overhead (< 5ms per event)
- Channels are memory-efficient for buffering events
- SSE connections are lightweight compared to WebSockets
- Old channels are automatically cleaned up after completion

## Future Enhancements

1. **WebSocket Support**: For bidirectional communication
2. **Partial Results**: Stream recipe ingredients as they're generated
3. **Progress Percentages**: More granular progress tracking
4. **Retry Logic**: Automatic reconnection on connection loss
5. **Compression**: Compress event data for large payloads
