namespace Infrastructure.Search.EventHandlers;

/// <summary>
/// FIX #8: همگام‌سازی IsInStock در Elasticsearch پس از تغییر موجودی
/// از payload کامل رویداد استفاده می‌کند تا از DB round-trip جلوگیری شود (FIX #10)
/// از Outbox pattern عبور می‌کند تا در صورت شکست retry شود
/// </summary>
public class InventoryStockSearchSyncHandler :
    INotificationHandler<VariantStockChangedEvent>,
    INotificationHandler<StockCommittedEvent>,
    INotificationHandler<StockReturnedEvent>
{
    private readonly Persistence.Context.DBContext _context;
    private readonly ILogger<InventoryStockSearchSyncHandler> _logger;

    public InventoryStockSearchSyncHandler(
        Persistence.Context.DBContext context,
        ILogger<InventoryStockSearchSyncHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Handle(VariantStockChangedEvent notification, CancellationToken cancellationToken)
    {
        await SyncProductAvailabilityAsync(notification.ProductId, cancellationToken);
    }

    public async Task Handle(StockCommittedEvent notification, CancellationToken cancellationToken)
    {
        
        var productId = await GetProductIdAsync(notification.VariantId, cancellationToken);
        if (productId.HasValue)
            await SyncProductAvailabilityAsync(productId.Value, cancellationToken);
    }

    public async Task Handle(StockReturnedEvent notification, CancellationToken cancellationToken)
    {
        var productId = await GetProductIdAsync(notification.VariantId, cancellationToken);
        if (productId.HasValue)
            await SyncProductAvailabilityAsync(productId.Value, cancellationToken);
    }

    private async Task SyncProductAvailabilityAsync(int productId, CancellationToken ct)
    {
        try
        {
            
            var hasStock = await _context.Set<Domain.Variant.ProductVariant>()
                .Where(v => v.ProductId == productId && v.IsActive && !v.IsDeleted)
                .AnyAsync(v => v.IsUnlimited || (v.StockQuantity - v.ReservedQuantity) > 0, ct);

            
            var outboxMessage = new ElasticsearchOutboxMessage
            {
                EntityType = "Product",
                EntityId = productId.ToString(),
                ChangeType = EntityChangeType.Updated.ToString(),
                
                Document = System.Text.Json.JsonSerializer.Serialize(new
                {
                    productId,
                    isInStock = hasStock,
                    syncReason = "StockChanged"
                }),
                CreatedAt = DateTime.UtcNow,
                RetryCount = 0
            };

            _context.ElasticsearchOutboxMessages.Add(outboxMessage);
            await _context.SaveChangesAsync(ct);

            _logger.LogDebug(
                "Queued Elasticsearch stock sync for Product {ProductId}, IsInStock={IsInStock}",
                productId, hasStock);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to queue Elasticsearch stock sync for Product {ProductId}", productId);
            
        }
    }

    private async Task<int?> GetProductIdAsync(int variantId, CancellationToken ct)
    {
        return await _context.Set<Domain.Variant.ProductVariant>()
            .Where(v => v.Id == variantId)
            .Select(v => (int?)v.ProductId)
            .FirstOrDefaultAsync(ct);
    }
}