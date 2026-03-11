using Domain.Brand;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Common.Abstractions;
using Domain.Product.ValueObjects;

namespace Domain.Product.Events;

public sealed record ProductCreatedEvent(
    ProductId ProductId,
    string Name,
    CategoryId CategoryId,
    BrandId BrandId) : IDomainEvent;