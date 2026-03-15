using Domain.Product.ValueObjects;

namespace Domain.Product.Events;

public sealed class ProductUpdatedEvent(
    ProductId ProductId,
    string Name,
    string Slug,
    string Description) : DomainEvent
{
    public ProductId ProductId { get; } = ProductId;
    public string Name { get; } = Name;
    public string Slug { get; } = Slug;
    public string Description { get; } = Description;
}