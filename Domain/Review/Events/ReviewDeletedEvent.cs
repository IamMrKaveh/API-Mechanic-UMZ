using Domain.Product.ValueObjects;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Review.Events;

public sealed record ReviewDeletedEvent(
    ReviewId ReviewId,
    ProductId ProductId,
    UserId UserId) : DomainEvent;
