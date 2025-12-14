namespace Infrastructure.Search.EventHandlers;

/// <summary>
/// Interface for entity change events
/// </summary>
public interface IEntityChangeEvent
{
    int EntityId { get; }
    string EntityType { get; }
    EntityChangeType ChangeType { get; }
}

public enum EntityChangeType
{
    Created,
    Updated,
    Deleted
}

/// <summary>
/// Product change event
/// </summary>
public record ProductChangedEvent(int EntityId, EntityChangeType ChangeType, ProductSearchDocument? Document = null)
    : IEntityChangeEvent
{
    public string EntityType => "Product";
}

/// <summary>
/// Category change event
/// </summary>
public record CategoryChangedEvent(int EntityId, EntityChangeType ChangeType, CategorySearchDocument? Document = null)
    : IEntityChangeEvent
{
    public string EntityType => "Category";
}

/// <summary>
/// CategoryGroup change event
/// </summary>
public record CategoryGroupChangedEvent(int EntityId, EntityChangeType ChangeType, CategoryGroupSearchDocument? Document = null)
    : IEntityChangeEvent
{
    public string EntityType => "CategoryGroup";
}

/// <summary>
/// Event handler for Elasticsearch synchronization
/// </summary>
public interface IElasticsearchEventHandler
{
    void HandleProductChangedAsync(ProductChangedEvent @event, CancellationToken ct = default);
    Task HandleCategoryChangedAsync(CategoryChangedEvent @event, CancellationToken ct = default);
    Task HandleCategoryGroupChangedAsync(CategoryGroupChangedEvent @event, CancellationToken ct = default);
}

public class ElasticsearchEventHandler : IElasticsearchEventHandler
{
    private readonly ISearchService _searchService;
    private readonly IElasticBulkService _bulkService;
    private readonly ILogger<ElasticsearchEventHandler> _logger;
    private readonly LedkaContext _context;

    public ElasticsearchEventHandler(
        ISearchService searchService,
        IElasticBulkService bulkService,
        ILogger<ElasticsearchEventHandler> logger,
        LedkaContext context)
    {
        _searchService = searchService;
        _bulkService = bulkService;
        _logger = logger;
        _context = context;
    }

    public void HandleProductChangedAsync(ProductChangedEvent @event, CancellationToken ct = default)
    {
        try
        {
            var outboxMessage = new ElasticsearchOutboxMessage
            {
                EntityType = "Product",
                EntityId = @event.EntityId.ToString(),
                ChangeType = @event.ChangeType.ToString(),
                Document = JsonSerializer.Serialize(@event.Document),
                CreatedAt = DateTime.UtcNow,
                RetryCount = 0
            };

            _context.ElasticsearchOutboxMessages.Add(outboxMessage);

            _logger.LogInformation(
                "Product {ProductId} change event saved to outbox for indexing",
                @event.EntityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save product change event to outbox");
            throw;
        }
    }

    public async Task HandleCategoryChangedAsync(CategoryChangedEvent @event, CancellationToken ct = default)
    {
        try
        {
            switch (@event.ChangeType)
            {
                case EntityChangeType.Created:
                case EntityChangeType.Updated:
                    if (@event.Document == null)
                    {
                        _logger.LogWarning("Category document is null for {ChangeType} event of category {CategoryId}",
                            @event.ChangeType, @event.EntityId);
                        return;
                    }
                    await _searchService.IndexCategoryAsync(@event.Document, ct);
                    _logger.LogInformation("Category {CategoryId} indexed successfully after {ChangeType}",
                        @event.EntityId, @event.ChangeType);
                    break;

                case EntityChangeType.Deleted:
                    // Note: Category deletion requires custom implementation based on your business logic
                    _logger.LogInformation("Category {CategoryId} deletion event received", @event.EntityId);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle category changed event for category {CategoryId}", @event.EntityId);
            throw;
        }
    }

    public async Task HandleCategoryGroupChangedAsync(CategoryGroupChangedEvent @event, CancellationToken ct = default)
    {
        try
        {
            switch (@event.ChangeType)
            {
                case EntityChangeType.Created:
                case EntityChangeType.Updated:
                    if (@event.Document == null)
                    {
                        _logger.LogWarning("CategoryGroup document is null for {ChangeType} event of group {GroupId}",
                            @event.ChangeType, @event.EntityId);
                        return;
                    }
                    await _searchService.IndexCategoryGroupAsync(@event.Document, ct);
                    _logger.LogInformation("CategoryGroup {GroupId} indexed successfully after {ChangeType}",
                        @event.EntityId, @event.ChangeType);
                    break;

                case EntityChangeType.Deleted:
                    // Note: CategoryGroup deletion requires custom implementation based on your business logic
                    _logger.LogInformation("CategoryGroup {GroupId} deletion event received", @event.EntityId);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle category group changed event for group {GroupId}", @event.EntityId);
            throw;
        }
    }
}

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
        _maxQueueSize = configuration.GetValue<int>("Elasticsearch:MaxEventQueueSize", 10000);
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