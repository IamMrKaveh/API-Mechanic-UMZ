using Domain.Common;

namespace Infrastructure.Common.Outbox;

/// <summary>
/// Background worker that reads unprocessed <see cref="OutboxMessage"/> rows and
/// republishes them as MediatR notifications, guaranteeing at-least-once delivery
/// even when the original process crashed after committing the transaction.
/// </summary>
public class OutboxPublisherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisherService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(15);
    private const int BatchSize = 100;

    public OutboxPublisherService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken st)
    {
        _logger.LogInformation("OutboxPublisherService started.");

        while (!st.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(st);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxPublisherService error.");
            }

            await Task.Delay(_interval, st);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                var eventType = Type.GetType(message.Type);
                if (eventType == null)
                {
                    _logger.LogWarning("Outbox: unknown event type {Type}", message.Type);
                    message.MarkFailed($"Unknown type: {message.Type}");
                }
                else
                {
                    var domainEvent = (INotification)System.Text.Json.JsonSerializer.Deserialize(message.Payload, eventType)!;
                    await mediator.Publish(domainEvent, ct);
                    message.MarkProcessed();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox: failed to publish message {Id}", message.Id);
                message.MarkFailed(ex.Message);
            }
        }

        if (messages.Count > 0)
            await dbContext.SaveChangesAsync(ct);
    }
}