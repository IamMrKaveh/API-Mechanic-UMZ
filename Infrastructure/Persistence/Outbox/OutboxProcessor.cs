namespace Infrastructure.Persistence.Outbox;

public sealed class OutboxProcessor(
    DBContext context,
    IPublisher publisher,
    IAuditService auditService) : IOutboxProcessor
{
    public async Task ProcessAsync(
    int batchSize = 50,
    CancellationToken ct = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await context.Database.BeginTransactionAsync(ct);

            var messages = await context.OutboxMessages
                .FromSqlRaw("""
                SELECT * FROM "OutboxMessages"
                WHERE "processed_at" IS NULL
                  AND "retry_count" < 5
                ORDER BY "created_at"
                LIMIT {0}
                FOR UPDATE SKIP LOCKED
                """, batchSize)
                .ToListAsync(ct);

            if (messages.Count == 0)
            {
                await transaction.RollbackAsync(ct);
                return;
            }

            foreach (var message in messages)
            {
                try
                {
                    var type = Type.GetType(message.Type);

                    if (type is null)
                    {
                        await auditService.LogWarningAsync(
                            $"Could not resolve type {message.Type} for outbox message {message.Id}",
                            ct);

                        message.MarkFailed($"Type not found: {message.Type}");
                        continue;
                    }

                    var @event = JsonSerializer.Deserialize(
                        message.Payload,
                        type);

                    if (@event is null)
                    {
                        message.MarkFailed("Deserialization returned null.");
                        continue;
                    }

                    var notificationType =
                        typeof(DomainEventNotification<>)
                            .MakeGenericType(type);

                    var notification =
                        Activator.CreateInstance(notificationType, @event)!;

                    await publisher.Publish(notification, ct);

                    message.MarkProcessed(DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    await auditService.LogErrorAsync(
                        $"Error processing outbox message {message.Id}: {ex.Message}",
                        ct);

                    message.MarkFailed(ex.Message);
                }
            }

            await context.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
        });
    }
}