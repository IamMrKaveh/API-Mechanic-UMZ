using Domain.Common.Abstractions;
using Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

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
            .Entries()
            .Where(e => e.Entity is not null)
            .Select(e => e.Entity)
            .OfType<AggregateRoot<object>>()
            .ToList();

        var allAggregateRoots = context.ChangeTracker
            .Entries()
            .Where(e => e.Entity is not null)
            .Select(e => e.Entity)
            .Where(e =>
            {
                var type = e.GetType();
                while (type != null)
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
                        return true;
                    type = type.BaseType;
                }
                return false;
            })
            .ToList();

        var domainEvents = new List<IDomainEvent>();

        foreach (var entity in allAggregateRoots)
        {
            var prop = entity.GetType().GetProperty(nameof(AggregateRoot<object>.DomainEvents));
            if (prop?.GetValue(entity) is IEnumerable<IDomainEvent> events)
                domainEvents.AddRange(events);
        }

        if (!domainEvents.Any()) return;

        var outboxMessages = domainEvents
            .Select(e => new OutboxMessage
            {
                Id = OutboxMessageId.NewId(),
                Type = e.GetType().FullName ?? e.GetType().Name,
                Payload = JsonSerializer.Serialize(e, e.GetType()),
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        foreach (var entity in allAggregateRoots)
        {
            var method = entity.GetType().GetMethod(nameof(AggregateRoot<object>.ClearDomainEvents));
            method?.Invoke(entity, null);
        }

        await context.Set<OutboxMessage>().AddRangeAsync(outboxMessages, ct);
        await context.SaveChangesAsync(ct);
    }
}