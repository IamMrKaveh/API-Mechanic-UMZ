namespace Infrastructure.Persistence;

public class DomainEventDispatcher(
    DBContext context,
    IMediator mediator,
    ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    private readonly DBContext _context = context;
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<DomainEventDispatcher> _logger = logger;

    public async Task DispatchEventsAsync(CancellationToken ct = default)
    {
        var domainEntities = _context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        foreach (var entity in domainEntities)
        {
            entity.Entity.ClearDomainEvents();
        }

        foreach (var domainEvent in domainEvents)
        {
            try
            {
                _logger.LogDebug(
                    "Dispatching domain event: {EventType}",
                    domainEvent.GetType().Name);

                await _mediator.Publish(domainEvent, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error dispatching domain event: {EventType}",
                    domainEvent.GetType().Name);
            }
        }
    }
}