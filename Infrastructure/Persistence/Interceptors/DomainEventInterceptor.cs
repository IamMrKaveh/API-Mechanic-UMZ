using Infrastructure.Persistence.Outbox;

namespace Infrastructure.Persistence.Interceptors;

public sealed class DomainEventInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken ct = default)
    {
        if (eventData.Context is not null)
            await DispatchDomainEventsToOutboxAsync(eventData.Context, ct);

        return await base.SavedChangesAsync(eventData, result, ct);
    }

    private static async Task DispatchDomainEventsToOutboxAsync(DbContext context, CancellationToken ct)
    {
        var aggregates = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        var outboxMessages = aggregates
            .SelectMany(a => a.DomainEvents)
            .Select(e => new OutboxMessage
            {
                Id = OutboxMessageId.NewId(),
                Type = e.GetType().FullName ?? e.GetType().Name,
                Payload = JsonSerializer.Serialize(e, e.GetType()),
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        if (outboxMessages.Count > 0)
        {
            await context.Set<OutboxMessage>().AddRangeAsync(outboxMessages, ct);
            await context.SaveChangesAsync(ct);
        }
    }
}