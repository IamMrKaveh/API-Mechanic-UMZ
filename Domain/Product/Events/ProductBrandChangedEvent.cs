using Domain.Brand;
using Domain.Brand.ValueObjects;
using Domain.Common.Abstractions;
using Domain.Product.ValueObjects;

namespace Domain.Product.Events;

public sealed record ProductBrandChangedEvent(
    ProductId ProductId,
    BrandId PreviousBrandId,
    BrandId NewBrandId) : IDomainEvent;