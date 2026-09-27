using Domain.Category.ValueObjects;

namespace Domain.Category.Events;

public sealed record CategoryActivatedEvent(CategoryId CategoryId) : DomainEvent;
