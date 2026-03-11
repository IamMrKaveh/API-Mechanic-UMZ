using Domain.Common.Abstractions;
using Domain.Product.ValueObjects;

namespace Domain.Product.Events;

public sealed record ProductUpdatedEvent(
    ProductId ProductId,
    string Name,
    string Slug,
    string Description) : IDomainEvent;