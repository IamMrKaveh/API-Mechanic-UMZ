namespace Infrastructure.Search.EventHandlers;

/// <summary>
/// Queue-based event processor for batching Elasticsearch updates
/// </summary>
public class ElasticsearchEventQueue(
    IConfiguration configuration,
    IAuditService auditService)
{
    private readonly ConcurrentQueue<IEntityChangeEvent> _eventQueue = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly int _maxQueueSize = configuration.GetValue("Elasticsearch:MaxEventQueueSize", 10000);
    private int _currentSize = 0;

    public bool EnqueueAsync(IEntityChangeEvent @event)
    {
        if (Interlocked.CompareExchange(ref _currentSize, 0, 0) >= _maxQueueSize)
        {
            auditService.LogWarningAsync(
                $"Event queue is full ({_currentSize}/{_maxQueueSize}).",
                CancellationToken.None);

            return false;
        }

        _eventQueue.Enqueue(@event);
        Interlocked.Increment(ref _currentSize);
        return true;
    }

    public async Task<List<IEntityChangeEvent>> DequeueAllAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var events = new List<IEntityChangeEvent>();
            while (_eventQueue.TryDequeue(out var @event))
            {
                events.Add(@event);
                Interlocked.Decrement(ref _currentSize);
            }

            _logger.LogInformation("Dequeued {Count} events from queue", events.Count);
            return events;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}