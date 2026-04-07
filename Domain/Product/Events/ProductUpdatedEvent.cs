using Domain.Product.ValueObjects;

namespace Domain.Product.Events;

public sealed class ProductUpdatedEvent(
    ProductId ProductId,
    ProductName productName,
    Slug Slug,
    string Description) : DomainEvent
{
    public ProductId ProductId { get; } = ProductId;
    public ProductName ProductName { get; } = productName;
    public Slug Slug { get; } = Slug;
    public string Description { get; } = Description;
}