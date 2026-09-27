using Domain.Product.ValueObjects;
using Domain.Review.ValueObjects;

namespace Domain.Review.Events;

public sealed record ReviewAdminRepliedEvent(
    ReviewId ReviewId,
    ProductId ProductId,
    string Reply) : DomainEvent;
