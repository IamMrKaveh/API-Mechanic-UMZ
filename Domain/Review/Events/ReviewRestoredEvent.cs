using Domain.Product.ValueObjects;
using Domain.Review.ValueObjects;

namespace Domain.Review.Events;

public sealed record ReviewRestoredEvent(
    ReviewId ReviewId,
    ProductId ProductId) : DomainEvent;
