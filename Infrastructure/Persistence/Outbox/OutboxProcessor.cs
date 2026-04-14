using Infrastructure.Persistence.Context;

namespace Infrastructure.Persistence.Outbox;

public sealed class OutboxProcessor(
    DBContext context,
    IPublisher publisher,
    ILogger<OutboxProcessor> logger) : IOutboxProcessor
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
                    logger.LogWarning("Could not resolve type {Type} for outbox message {Id}", message.Type, message.Id);
                    message.ProcessedAt = DateTime.UtcNow;
                    message.Error = $"Type not found: {message.Type}";
                    continue;
                }

                var @event = JsonSerializer.Deserialize(message.Payload, type);
                if (@event is null)
                {
                    message.ProcessedAt = DateTime.UtcNow;
                    message.Error = "Deserialization returned null.";
                    continue;
                }

                await publisher.Publish(@event, ct);
                message.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox message {Id}", message.Id);
                message.RetryCount++;
                message.Error = ex.Message;
            }
        }

        await context.SaveChangesAsync(ct);
    }
}