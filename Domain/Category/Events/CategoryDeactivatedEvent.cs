using Domain.Category.ValueObjects;

namespace Domain.Category.Events;

public sealed record CategoryDeactivatedEvent(CategoryId CategoryId) : DomainEvent;
