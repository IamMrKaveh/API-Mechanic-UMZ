using Domain.Product.ValueObjects;
using Domain.Review.ValueObjects;

namespace Domain.Review.Events;

public sealed record ReviewRejectedEvent(
    ReviewId ReviewId,
    ProductId ProductId,
    string? Reason) : DomainEvent;
