namespace Infrastructure.Search.EventHandlers;

/// <summary>
/// Queue-based event processor for batching Elasticsearch updates
/// </summary>
public class ElasticsearchEventQueue
{
    private readonly ConcurrentQueue<IEntityChangeEvent> _eventQueue = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ILogger<ElasticsearchEventQueue> _logger;
    private readonly int _maxQueueSize;
    private int _currentSize = 0;

    public ElasticsearchEventQueue(
        ILogger<ElasticsearchEventQueue> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _maxQueueSize = configuration.GetValue("Elasticsearch:MaxEventQueueSize", 10000);
    }

    public bool EnqueueAsync(IEntityChangeEvent @event)
    {
        if (Interlocked.CompareExchange(ref _currentSize, 0, 0) >= _maxQueueSize)
        {
            _logger.LogWarning(
                "Event queue is full ({CurrentSize}/{MaxSize}). Dropping event: {EntityType} {EntityId}",
                _currentSize, _maxQueueSize, @event.EntityType, @event.EntityId);
            return false;
        }

        _eventQueue.Enqueue(@event);
        Interlocked.Increment(ref _currentSize);

        _logger.LogDebug(
            "Event enqueued: {EntityType} {EntityId} {ChangeType}. Queue size: {QueueSize}",
            @event.EntityType, @event.EntityId, @event.ChangeType, _currentSize);

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

    public int Count => _currentSize;

    public int MaxQueueSize => _maxQueueSize;
}