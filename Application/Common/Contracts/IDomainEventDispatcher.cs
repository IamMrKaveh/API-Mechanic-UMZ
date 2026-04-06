using Domain.Common.Abstractions;

namespace Application.Common.Contracts;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken ct = default);
}