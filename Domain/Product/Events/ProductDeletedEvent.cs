using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Product.Events;

public sealed class ProductDeletedEvent(ProductId productId, UserId? deletedBy) : DomainEvent
{
    public ProductId ProductId { get; } = productId;
    public UserId? DeletedBy { get; } = deletedBy;
}