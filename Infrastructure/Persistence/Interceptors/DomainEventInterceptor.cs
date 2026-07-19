using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using Domain.Common.Abstractions;
using Infrastructure.Persistence.Outbox;

namespace Infrastructure.Persistence.Interceptors;

public sealed class DomainEventInterceptor(
    IOutboxEventTypeRegistry typeRegistry) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

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
                DateTime.UtcNow,
                traceParent,
                traceState));
        }

        if (outboxMessages.Count > 0)
            await dbContext.OutboxMessages.AddRangeAsync(outboxMessages, ct);
    }
}
