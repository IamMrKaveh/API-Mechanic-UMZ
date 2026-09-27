using Domain.Product.ValueObjects;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Review.Events;

public sealed record ReviewSubmittedEvent(
    ReviewId ReviewId,
    ProductId ProductId,
    UserId UserId,
    Rating Rating) : DomainEvent;
