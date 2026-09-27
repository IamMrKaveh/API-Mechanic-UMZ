using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.ValueObjects;

namespace Domain.Product.Events;

public sealed record ProductCreatedEvent(
ProductId ProductId,
    ProductName ProductName,
    BrandId BrandId,
    CategoryId CategoryId) : DomainEvent;
