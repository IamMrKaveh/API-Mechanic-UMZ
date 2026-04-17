using Domain.Common.Abstractions;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Persistence.Interceptors;

public sealed class DomainEventInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        DispatchDomainEvents(eventData.Context).GetAwaiter().GetResult();
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await DispatchDomainEvents(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static async Task DispatchDomainEvents(DbContext? context)
    {
        if (context is null) return;

        var aggregates = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        var outboxMessages = domainEvents.Select(domainEvent =>
        {
            var type = domainEvent.GetType().AssemblyQualifiedName!;
            var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());

            return OutboxMessage.Create(type, payload);
        }).ToList();

        if (context is DBContext dbContext)
            await dbContext.OutboxMessages.AddRangeAsync(outboxMessages);
    }
}