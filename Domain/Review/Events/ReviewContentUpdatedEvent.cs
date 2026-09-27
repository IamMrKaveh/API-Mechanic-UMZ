using Domain.Product.ValueObjects;
using Domain.Review.ValueObjects;

namespace Domain.Review.Events;

public sealed record ReviewContentUpdatedEvent(
    ReviewId ReviewId,
    ProductId ProductId,
    int NewRating) : DomainEvent;
