using Infrastructure.Persistence.Outbox;

namespace Infrastructure.Common.Outbox;

/// <summary>
/// Background worker that reads unprocessed <see cref="OutboxMessage"/> rows and
/// republishes them as MediatR notifications, guaranteeing at-least-once delivery
/// even when the original process crashed after committing the transaction.
/// </summary>
public class OutboxPublisherService(
    IServiceScopeFactory scopeFactory,
    IAuditService auditService) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(15);
    private const int BatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken st)
    {
        await auditService.LogInformationAsync("OutboxPublisherService started.", st);

        while (!st.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(st);
            }
            catch (Exception)
            {
                await auditService.LogErrorAsync("OutboxPublisherService error.", st);
            }

            await Task.Delay(_interval, st);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
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
                    await auditService.LogWarningAsync("Outbox: unknown event type {Type}", message.Type);
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
                await auditService.LogErrorAsync("Outbox: failed to publish message {Id}", message.Id);
                message.MarkFailed(ex.Message);
            }
        }

        if (messages.Count > 0)
            await dbContext.SaveChangesAsync(ct);
    }
}