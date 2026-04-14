using Application.Common.Contracts;
using Domain.Common.Abstractions;

namespace Infrastructure.Persistence;

public sealed class DomainEventDispatcher(IPublisher publisher) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var @event in events)
        {
            await publisher.Publish(@event, ct);
        }
    }
}