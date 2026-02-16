namespace Infrastructure.Search.EventHandlers;

public class ElasticsearchEventHandler : IElasticsearchEventHandler
{
    private readonly ISearchService _searchService;
    private readonly IElasticBulkService _bulkService;
    private readonly ILogger<ElasticsearchEventHandler> _logger;
    private readonly LedkaContext _context;
    private readonly ElasticsearchClient _elasticClient;

    public ElasticsearchEventHandler(
        ISearchService searchService,
        IElasticBulkService bulkService,
        ILogger<ElasticsearchEventHandler> logger,
        LedkaContext context,
        ElasticsearchClient elasticClient)
    {
        _searchService = searchService;
        _bulkService = bulkService;
        _logger = logger;
        _context = context;
        _elasticClient = elasticClient;
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
                    var deleteResponse = await _elasticClient.DeleteAsync<CategorySearchDocument>(@event.EntityId.ToString(), d => d.Index("categories_v1"), ct);
                    if (!deleteResponse.IsValidResponse)
                        _logger.LogWarning("Failed to delete Category {CategoryId} from index: {Error}", @event.EntityId, deleteResponse.DebugInformation);
                    else
                        _logger.LogInformation("Category {CategoryId} deleted from index", @event.EntityId);
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
                    var deleteResponse = await _elasticClient.DeleteAsync<CategoryGroupSearchDocument>(@event.EntityId.ToString(), d => d.Index("categorygroups_v1"), ct);
                    if (!deleteResponse.IsValidResponse)
                        _logger.LogWarning("Failed to delete CategoryGroup {GroupId} from index: {Error}", @event.EntityId, deleteResponse.DebugInformation);
                    else
                        _logger.LogInformation("CategoryGroup {GroupId} deleted from index", @event.EntityId);
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