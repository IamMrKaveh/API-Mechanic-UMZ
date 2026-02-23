namespace Infrastructure.Search;

public class ElasticDeadLetterQueue : IElasticDeadLetterQueue
{
    private readonly Persistence.Context.DBContext _context;
    private readonly ILogger<ElasticDeadLetterQueue> _logger;

    public ElasticDeadLetterQueue(
        Persistence.Context.DBContext context,
        ILogger<ElasticDeadLetterQueue> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task EnqueueAsync(FailedIndexOperation operation, CancellationToken ct)
    {
        try
        {
            var entity = new FailedElasticOperation
            {
                EntityType = operation.EntityType,
                EntityId = operation.EntityId,
                Document = operation.Document,
                Error = operation.Error,
                CreatedAt = operation.Timestamp,
                RetryCount = 0,
                LastRetryAt = null,
                Status = "Pending"
            };

            _context.FailedElasticOperations.Add(entity);
            await _context.SaveChangesAsync(ct);

            _logger.LogWarning(
                "Enqueued failed operation to DLQ: {EntityType} {EntityId}",
                operation.EntityType,
                operation.EntityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue to dead letter queue");
        }
    }

    public async Task<IEnumerable<FailedIndexOperation>> DequeueAsync(int count, CancellationToken ct)
    {
        var operations = await _context.FailedElasticOperations
            .Where(o => o.Status == "Pending" && o.RetryCount < 5)
            .OrderBy(o => o.CreatedAt)
            .Take(count)
            .ToListAsync(ct);

        return operations.Select(o => new FailedIndexOperation
        {
            EntityType = o.EntityType,
            EntityId = o.EntityId,
            Document = o.Document,
            Error = o.Error,
            Timestamp = o.CreatedAt,
            RetryCount = o.RetryCount
        });
    }
}