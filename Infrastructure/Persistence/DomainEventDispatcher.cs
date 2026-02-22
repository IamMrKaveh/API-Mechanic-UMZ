namespace Infrastructure.Persistence;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly LedkaContext _context;
    private readonly IMediator _mediator;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(
        LedkaContext context,
        IMediator mediator,
        ILogger<DomainEventDispatcher> logger)
    {
        _context = context;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task DispatchEventsAsync(CancellationToken cancellationToken = default)
    {
        var domainEntities = _context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        // پاکسازی رویدادها قبل از dispatch (جلوگیری از حلقه بی‌نهایت)
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

                await _mediator.Publish(domainEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error dispatching domain event: {EventType}",
                    domainEvent.GetType().Name);
                // در صورت نیاز می‌توان throw کرد یا به Dead Letter Queue اضافه کرد
            }
        }
    }
}