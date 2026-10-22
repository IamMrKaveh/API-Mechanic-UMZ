using System.Diagnostics;
using System.Text.Json;
using Domain.Common.Abstractions;
using Infrastructure.Persistence.Outbox;

namespace Infrastructure.Persistence.Interceptors;

public sealed class DomainEventInterceptor(
    IOutboxEventTypeRegistry typeRegistry) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions SerializerOptions = DomainEventJson.Options;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(DbContext? context, CancellationToken ct)
    {
        if (context is not DBContext dbContext) return;

        var aggregates = dbContext.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        if (aggregates.Count == 0) return;

        var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        var currentActivity = Activity.Current;
        var traceParent = currentActivity?.Id;
        var traceState = currentActivity?.TraceStateString;

        var outboxMessages = new List<OutboxMessage>(domainEvents.Count);

        // All the messages of one transaction share the same timestamp, and the
        // sequence keeps the order in which the events were raised, so the
        // processor always publishes them in that order.
        var createdAt = DateTime.UtcNow;
        var sequence = 0;

        foreach (var domainEvent in domainEvents)
        {
            string typeName;
            string payload;
            try
            {
                typeName = typeRegistry.GetTypeName(domainEvent.GetType());
                payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions);
            }
            catch
            {
                continue;
            }

            outboxMessages.Add(OutboxMessage.Create(
                typeName,
                payload,
                createdAt,
                traceParent,
                traceState,
                sequence++));
        }

        if (outboxMessages.Count > 0)
            await dbContext.OutboxMessages.AddRangeAsync(outboxMessages, ct);
    }
}
