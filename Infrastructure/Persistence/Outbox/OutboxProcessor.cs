namespace Infrastructure.Persistence.Outbox;

public sealed class OutboxProcessor(
    DBContext context,
    IPublisher publisher,
    IAuditService auditService) : IOutboxProcessor
{
    public async Task ProcessAsync(int batchSize = 50, CancellationToken ct = default)
    {
        var messages = await context.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                var type = Type.GetType(message.Type);
                if (type is null)
                {
                    await auditService.LogWarningAsync(
                        $"Could not resolve type {message.Type} for outbox message {message.Id}", ct);
                    message.MarkFailed($"Type not found: {message.Type}");
                    continue;
                }

                var @event = JsonSerializer.Deserialize(message.Payload, type);
                if (@event is null)
                {
                    message.MarkFailed("Deserialization returned null.");
                    continue;
                }

                var notificationType = typeof(DomainEventNotification<>).MakeGenericType(type);
                var notification = Activator.CreateInstance(notificationType, @event)!;
                await publisher.Publish(notification, ct);

                message.MarkProcessed(DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                await auditService.LogErrorAsync(
                    $"Error processing outbox message {message.Id}: {ex.Message}", ct);
                message.MarkFailed(ex.Message);
            }
        }

        await context.SaveChangesAsync(ct);
    }
}