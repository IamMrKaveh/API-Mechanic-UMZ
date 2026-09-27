using Domain.Product.ValueObjects;

namespace Domain.Product.Events;

public sealed record ProductDeactivatedEvent(ProductId ProductId) : DomainEvent;
