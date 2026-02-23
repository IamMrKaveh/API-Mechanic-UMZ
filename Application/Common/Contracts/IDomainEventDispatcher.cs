namespace Application.Common.Contracts;

/// <summary>
/// انتشار Domain Events پس از SaveChangesAsync
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(
        CancellationToken ct = default
        );
}