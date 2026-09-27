using Domain.Product.ValueObjects;

namespace Domain.Product.Events;

public sealed record ProductUpdatedEvent(
    ProductId ProductId,
    ProductName ProductName,
    ProductSlug Slug,
    string Description) : DomainEvent;
