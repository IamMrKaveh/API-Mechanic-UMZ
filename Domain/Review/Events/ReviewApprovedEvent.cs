using Domain.Product.ValueObjects;
using Domain.Review.ValueObjects;

namespace Domain.Review.Events;

public sealed record ReviewApprovedEvent(
    ReviewId ReviewId,
    ProductId ProductId,
    Rating Rating) : DomainEvent;
